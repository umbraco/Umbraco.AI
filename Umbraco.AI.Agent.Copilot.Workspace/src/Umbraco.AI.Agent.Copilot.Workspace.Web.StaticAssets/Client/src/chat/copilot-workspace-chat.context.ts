import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import {
    UaiRunController,
    UaiToolRendererManager,
    UaiFrontendToolManager,
    UaiHitlContext,
    UAI_HITL_CONTEXT,
    UAI_CHAT_CONTEXT,
    UaiHitlInterruptHandler,
    UaiDefaultInterruptHandler,
    type UaiChatContextApi,
    type UaiAgentItem,
    type UaiInputContent,
} from "@umbraco-ai/agent-ui";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";
import type { ConversationResponseModel } from "../conversation/types.js";
import {
    UAI_CONVERSATION_WORKSPACE_CONTEXT,
    type UaiConversationTarget,
    type UaiConversationWorkspaceContext,
} from "../conversation/workspace/conversation-workspace.context.js";
import { UaiServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiWorkspaceAgentRepository } from "./workspace-agent.repository.js";
import { stashPendingFirstMessage, takePendingFirstMessage } from "./pending-first-message.js";
import { copilotWorkspaceConversationPath, navigateToWorkspacePath } from "../paths.js";

/** The "Auto" agent option — persisted as agentIdOrAlias "auto"; the backend then auto-selects. */
const AUTO_AGENT: UaiAgentItem = { id: "auto", name: "Auto", alias: "auto" };

/** Max length of an auto-derived conversation title before it's truncated with an ellipsis. */
const AUTO_TITLE_MAX_LENGTH = 60;

/** Derives a conversation title from the first user message (collapsed whitespace, truncated). */
function deriveConversationTitle(content: string): string {
    const text = content.trim().replace(/\s+/g, " ");
    if (!text) return "";
    return text.length > AUTO_TITLE_MAX_LENGTH ? `${text.slice(0, AUTO_TITLE_MAX_LENGTH).trimEnd()}…` : text;
}

/** A stable key for a workspace target, to skip re-initialising the run controller for the same one. */
function targetKey(target: UaiConversationTarget): string {
    return target.isDraft ? "draft" : (target.id ?? "none");
}

/**
 * Chat runtime for the Copilot Workspace. Implements `UaiChatContextApi` so the shared `<uai-chat>`
 * subtree drives off it, wrapping a `UaiRunController` on the server-persisted conversation strategy
 * (durable history; the client sends only the new turn).
 *
 * It is NOT the source of truth for *which* conversation is open — that is the workspace store
 * ({@link UaiConversationWorkspaceContext}), which it consumes: it re-keys its thread reactively off the
 * store's {@link UaiConversationWorkspaceContext.target$} and resolves the agent picker off
 * `conversation$`. It owns only the chat mechanics: the run controller, the agent picker, sending, and
 * draft→create-on-first-message promotion.
 */
export class UaiCopilotWorkspaceChatContext extends UmbControllerBase implements UaiChatContextApi {
    public readonly IS_COPILOT_WORKSPACE_CONTEXT = true;

    #conversationRepository: UaiConversationRepository;
    #agentRepository: UaiWorkspaceAgentRepository;
    #strategy: UaiServerPersistedConversationStrategy;
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    #toolRendererManager: UaiToolRendererManager;

    #store?: UaiConversationWorkspaceContext;

    #agents = new UmbArrayState<UaiAgentItem>([], (x) => x.id);
    #selectedAgent = new UmbBasicState<UaiAgentItem | undefined>(undefined);
    #agentsLoading = new UmbBooleanState(false);

    /** Local mirror of the store's open conversation, for agent resolution + auto-titling. */
    #conversation?: ConversationResponseModel;
    /** The target the run controller is currently keyed to (guards duplicate re-inits). */
    #currentTargetKey?: string;
    /** Guards against a second send racing the create request during draft promotion. */
    #creating = false;

    readonly agents = this.#agents.asObservable();
    readonly selectedAgent = this.#selectedAgent.asObservable();
    readonly agentsLoading = this.#agentsLoading.asObservable();

    get messages$() {
        return this.#runController.messages$;
    }
    get streamingContent$() {
        return this.#runController.streamingContent$;
    }
    get agentState$() {
        return this.#runController.agentState$;
    }
    get isRunning$() {
        return this.#runController.isRunning$;
    }
    get resolvedAgent$() {
        return this.#runController.resolvedAgent$;
    }
    get toolRendererManager(): UaiToolRendererManager {
        return this.#toolRendererManager;
    }
    get hitlInterrupt$() {
        return this.#hitlContext.interrupt$;
    }
    get pendingApproval$() {
        return this.#hitlContext.pendingApproval$;
    }

    constructor(host: UmbControllerHost) {
        super(host);

        this.#conversationRepository = new UaiConversationRepository(host);
        this.#agentRepository = new UaiWorkspaceAgentRepository(host);
        this.#hitlContext = new UaiHitlContext(host);
        this.#toolRendererManager = new UaiToolRendererManager(host);
        this.#strategy = new UaiServerPersistedConversationStrategy(this.#conversationRepository);
        const frontendToolManager = new UaiFrontendToolManager(host);

        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager: this.#toolRendererManager,
            frontendToolManager,
            conversationStrategy: this.#strategy,
            interruptHandlers: [new UaiHitlInterruptHandler(this), new UaiDefaultInterruptHandler()],
        });

        // Maintain the picker's agent list (with an "Auto" option when >1 agent), keeping the current
        // selection valid as the catalog loads/changes. Agents load asynchronously and often arrive AFTER
        // a conversation opens (empty list, no selection), so default-select whenever there's no valid
        // current pick — otherwise the picker stays empty even though agents exist.
        this.observe(this.#agentRepository.agentItems$, (agents) => {
            const displayAgents = agents.length > 1 ? [AUTO_AGENT, ...agents] : [...agents];
            this.#agents.setValue(displayAgents);

            const selected = this.#selectedAgent.getValue();
            const stillValid = selected && displayAgents.some((a) => a.id === selected.id);
            if (!stillValid && displayAgents.length > 0) {
                this.#selectedAgent.setValue(this.#resolveSelectedAgent(displayAgents));
            }
        });

        this.provideContext(UAI_CHAT_CONTEXT, this);
        this.provideContext(UAI_HITL_CONTEXT, this.#hitlContext);

        // React to the store: re-key the thread when the open target changes, and resync the agent
        // picker when the loaded conversation (its stored agent choice) arrives/changes.
        this.consumeContext(UAI_CONVERSATION_WORKSPACE_CONTEXT, (store) => {
            this.#store = store;
            this.observe(store?.target$, (target) => target && void this.#syncTarget(target));
            this.observe(store?.conversation$, (conversation) => {
                this.#conversation = conversation;
                this.#selectedAgent.setValue(this.#resolveSelectedAgent(this.#agents.getValue()));
            });
        });
    }

    async loadAgents(): Promise<void> {
        this.#agentsLoading.setValue(true);
        await this.#agentRepository.initialize();
        this.#agentsLoading.setValue(false);
    }

    /**
     * Re-keys the run controller to the store's current target — a persisted conversation (reset the
     * thread to a conversation-scoped client, load its history, then replay a draft's stashed first turn)
     * or a fresh draft (empty thread). No-op for the target already keyed. The real agent is resolved
     * server-side, so a synthetic per-target agent id is only used to force a client/thread reset.
     */
    async #syncTarget(target: UaiConversationTarget): Promise<void> {
        // The store's initial target is "nothing open yet" (not a draft, no id) — ignore it; the route
        // sets a real target moments later.
        if (!target.isDraft && !target.id) return;
        const key = targetKey(target);
        if (key === this.#currentTargetKey) return;
        this.#currentTargetKey = key;
        this.#creating = false;

        this.#runController.abortRun();

        if (target.isDraft) {
            this.#strategy.setConversationId(undefined);
            this.#runController.setAgent({ id: "conversation:new", name: "Workspace", alias: "workspace" });
            await this.#runController.loadInitialMessages();
            return;
        }

        const id = target.id!;
        this.#strategy.setConversationId(id);
        this.#runController.setAgent({ id: `conversation:${id}`, name: "Workspace", alias: "workspace" });
        await this.#runController.loadInitialMessages();

        // If this open is the promotion of a draft, replay the turn stashed before navigation, now that
        // the (empty) history has loaded so the send appends onto it.
        const pending = takePendingFirstMessage(id);
        if (pending) await this.sendUserMessage(pending.content, pending.contentParts);
    }

    /** Resolves which picker option should be selected from the conversation's stored agent choice. */
    #resolveSelectedAgent(available: UaiAgentItem[]): UaiAgentItem | undefined {
        const stored = this.#conversation?.agentIdOrAlias ?? undefined;
        if (stored && stored !== "auto") {
            const match = available.find((a) => a.id === stored || a.alias === stored);
            if (match) return match;
        }
        return available.find((a) => a.id === AUTO_AGENT.id) ?? available[0];
    }

    selectAgent(agentId: string | undefined): void {
        const agent = agentId ? this.#agents.getValue().find((a) => a.id === agentId) : undefined;
        this.#selectedAgent.setValue(agent);

        // Persist the choice onto the conversation (via the store) so the server resolves it next turn.
        // A draft has no conversation yet; the pick is applied when it's created (see the handoff below).
        if (!this.#conversation) return;
        this.#store?.setAgentIdOrAlias(!agent || agent.id === AUTO_AGENT.id ? "auto" : agent.id);
    }

    respondToHitl(response: string): void {
        this.#hitlContext.respond(response);
    }

    async sendUserMessage(content: string, contentParts?: UaiInputContent[]): Promise<void> {
        // A draft has no conversation yet — create it now (only the first message persists a conversation),
        // then hand the turn to the freshly-opened real view to stream.
        if (this.#store?.isDraft()) {
            await this.#createFromDraftAndHandoff(content, contentParts);
            return;
        }
        // Title an untitled conversation from its first message (fire-and-forget; doesn't delay the send).
        this.#maybeAutoTitle(content);
        // Project context is injected server-side from the conversation; no client context needed.
        this.#runController.sendUserMessage(content, [], contentParts);
    }

    /**
     * Persists the draft on its first message: creates the conversation (title derived from the message,
     * the picked agent/project applied up front), stashes the turn, and navigates to the real conversation.
     * Opening that route re-keys this context (via the store target) and replays the stashed turn — so it
     * streams through the normal path.
     */
    async #createFromDraftAndHandoff(content: string, contentParts?: UaiInputContent[]): Promise<void> {
        if (this.#creating) return;
        this.#creating = true;

        const selected = this.#selectedAgent.getValue();
        const agentIdOrAlias = selected && selected.id !== AUTO_AGENT.id ? selected.id : undefined;

        const { data } = await this.#conversationRepository.create({
            projectId: this.#store?.getDraftProjectId(),
            title: deriveConversationTitle(content) || undefined,
            agentIdOrAlias,
        });

        if (!data?.id) {
            // Create failed — stay in draft so the user can retry (matches the prior new-chat behaviour).
            this.#creating = false;
            return;
        }

        stashPendingFirstMessage(data.id, { content, contentParts });
        navigateToWorkspacePath(copilotWorkspaceConversationPath(data.id));
    }

    /**
     * On the first message of an untitled conversation, derives a title from it and persists it via the
     * store (which updates the shared conversation state → the menu refreshes). Guards against re-titling.
     */
    #maybeAutoTitle(content: string): void {
        const conversation = this.#conversation;
        if (!conversation || conversation.title?.trim()) return;
        const title = deriveConversationTitle(content);
        if (title) this.#store?.rename(title);
    }

    abortRun(): void {
        this.#runController.abortRun();
    }

    regenerateLastMessage(): void {
        this.#runController.regenerateLastMessage();
    }
}

export const UAI_COPILOT_WORKSPACE_CHAT_CONTEXT = new UmbContextToken<UaiCopilotWorkspaceChatContext>(
    "UaiCopilotWorkspaceChatContext",
    undefined,
    (context): context is UaiCopilotWorkspaceChatContext =>
        (context as UaiCopilotWorkspaceChatContext).IS_COPILOT_WORKSPACE_CONTEXT,
);

export default UaiCopilotWorkspaceChatContext;
