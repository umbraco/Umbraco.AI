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
import { UaiServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiWorkspaceAgentRepository } from "./workspace-agent.repository.js";
import { stashPendingFirstMessage } from "./pending-first-message.js";
import { copilotWorkspaceConversationPath, navigateToWorkspacePath } from "../paths.js";
import { UaiConversationUpdatedController } from "../conversation/conversation-updated.controller.js";

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

/**
 * Chat context for the Copilot Workspace section. Implements `UaiChatContextApi` so the shared
 * `<uai-chat>` subtree drives off it, wrapping a `UaiRunController` configured with the
 * server-persisted conversation strategy (durable history; the client sends only the new turn).
 *
 * Differs from the contextual Copilot context: no entity/adapter context (system-wide chat), and the
 * active conversation is switchable via {@link setConversation}. The agent picker persists the user's
 * choice onto the conversation (`agentIdOrAlias`); the backend resolves the actual agent server-side,
 * so agent selection does NOT reset the run controller (which would wipe the loaded history).
 */
export class UaiCopilotWorkspaceChatContext extends UmbControllerBase implements UaiChatContextApi {
    public readonly IS_COPILOT_WORKSPACE_CONTEXT = true;

    #conversationRepository: UaiConversationRepository;
    #agentRepository: UaiWorkspaceAgentRepository;
    #strategy: UaiServerPersistedConversationStrategy;
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    #toolRendererManager: UaiToolRendererManager;

    #agents = new UmbArrayState<UaiAgentItem>([], (x) => x.id);
    #selectedAgent = new UmbBasicState<UaiAgentItem | undefined>(undefined);
    #agentsLoading = new UmbBooleanState(false);
    /** True when the open conversation is archived — the shared chat renders read-only in that case. */
    #readonly = new UmbBooleanState(false);
    /**
     * False while a conversation's mode is still being resolved (its metadata is loading), so the chat
     * can withhold the composer/read-only notice until it knows which to show — otherwise the composer
     * flashes for the moment before an archived conversation resolves to read-only.
     */
    #ready = new UmbBooleanState(false);

    #conversationId?: string;
    #conversation?: ConversationResponseModel;

    /** Draft mode: no conversation exists yet; it's created on the first sent message. */
    #isDraft = false;
    #draftProjectId?: string;
    /** Guards against a second send racing the create request during draft promotion. */
    #creating = false;

    readonly agents = this.#agents.asObservable();
    readonly selectedAgent = this.#selectedAgent.asObservable();
    readonly agentsLoading = this.#agentsLoading.asObservable();
    /** Observable read-only flag: true while the open conversation is archived. */
    readonly isReadonly$ = this.#readonly.asObservable();
    /** Observable: true once the open conversation's mode (editable vs read-only) is known. */
    readonly isReady$ = this.#ready.asObservable();

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

        // Maintain the picker's agent list (with an "Auto" option when >1 agent), keeping the
        // current selection valid as the catalog loads/changes. Agents load asynchronously and often
        // arrive AFTER setConversation()/startDraft() have already run (with an empty list, leaving no
        // selection), so default-select whenever there's no valid current pick — not only when an
        // existing pick was invalidated. Otherwise the picker stays empty even though agents exist.
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

        // React to the open conversation being archived/unarchived elsewhere (e.g. its sidebar ⋯ menu)
        // so the view flips its read-only state in place, without reloading the chat history.
        new UaiConversationUpdatedController(this, () => this.#conversationId, () => void this.#refreshReadonly());
    }

    /** Re-reads the open conversation's archived flag and flips read-only, leaving history untouched. */
    async #refreshReadonly(): Promise<void> {
        if (!this.#conversationId) return;
        const { data } = await this.#conversationRepository.requestById(this.#conversationId);
        if (!data || data.id !== this.#conversationId) return;
        this.#applyLoadedConversation(data);
    }

    /** Applies a freshly-loaded conversation's metadata that affects presentation (read-only state). */
    #applyLoadedConversation(data: ConversationResponseModel | undefined): void {
        this.#conversation = data ?? undefined;
        this.#readonly.setValue(data?.isArchived ?? false);
    }

    async loadAgents(): Promise<void> {
        this.#agentsLoading.setValue(true);
        await this.#agentRepository.initialize();
        this.#agentsLoading.setValue(false);
    }

    /**
     * Opens a conversation: binds the strategy, (re)creates the conversation-keyed client (which
     * resets the thread), syncs the agent picker from the stored choice, then seeds the thread with
     * persisted history. Re-entrant/no-op for the already-open conversation.
     */
    async setConversation(conversationId: string): Promise<void> {
        if (this.#conversationId === conversationId) return;
        this.#conversationId = conversationId;
        this.#isDraft = false;
        this.#draftProjectId = undefined;
        // Mode not yet known — withhold the composer until the conversation resolves (avoids a flash of
        // the input before an archived conversation switches to read-only).
        this.#ready.setValue(false);

        this.#runController.abortRun();
        this.#strategy.setConversationId(conversationId);

        // A synthetic per-conversation agent id makes setAgent recreate the (conversation-keyed)
        // client and reset the thread; the real agent is resolved server-side.
        this.#runController.setAgent({ id: `conversation:${conversationId}`, name: "Workspace", alias: "workspace" });

        const { data } = await this.#conversationRepository.requestById(conversationId);
        this.#applyLoadedConversation(data ?? undefined);
        this.#ready.setValue(true);
        this.#selectedAgent.setValue(this.#resolveSelectedAgent(this.#agents.getValue()));

        await this.#runController.loadInitialMessages();
    }

    /**
     * Starts a new draft conversation: nothing is persisted yet. Presents an empty thread with a working
     * agent picker; the conversation is created (and this context promoted) on the first sent message
     * via {@link sendUserMessage}. An optional `projectId` pre-attaches the eventual conversation.
     */
    async startDraft(projectId?: string): Promise<void> {
        this.#conversationId = undefined;
        this.#conversation = undefined;
        this.#isDraft = true;
        this.#draftProjectId = projectId;
        this.#creating = false;
        this.#readonly.setValue(false);
        // A draft is always editable; its mode is known immediately (no metadata fetch).
        this.#ready.setValue(true);

        this.#runController.abortRun();
        this.#strategy.setConversationId(undefined);
        // Fresh synthetic id → recreate the client and reset the thread to empty (no persisted history).
        this.#runController.setAgent({ id: "conversation:new", name: "Workspace", alias: "workspace" });
        this.#selectedAgent.setValue(this.#resolveSelectedAgent(this.#agents.getValue()));

        await this.#runController.loadInitialMessages();
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

        // Persist the choice onto the conversation so the server resolves it on the next turn.
        const conversation = this.#conversation;
        if (!conversation) return;
        const agentIdOrAlias = !agent || agent.id === AUTO_AGENT.id ? "auto" : agent.id;
        this.#conversation = { ...conversation, agentIdOrAlias };
        void this.#conversationRepository.setAgentIdOrAlias(conversation, agentIdOrAlias);
    }

    respondToHitl(response: string): void {
        this.#hitlContext.respond(response);
    }

    async sendUserMessage(content: string, contentParts?: UaiInputContent[]): Promise<void> {
        // A draft has no conversation yet — create it now (only the first message persists a conversation),
        // then hand the turn to the freshly-opened real view to stream.
        if (this.#isDraft) {
            await this.#createFromDraftAndHandoff(content, contentParts);
            return;
        }
        // Title an untitled conversation from its first message (fire-and-forget; doesn't delay the send).
        this.#maybeAutoTitle(content);
        // Project context is injected server-side from the conversation; no client context needed.
        this.#runController.sendUserMessage(content, [], contentParts);
    }

    /**
     * Persists the draft on its first message: creates the conversation (with a title derived from the
     * message and the picked agent/project applied up front, so it never shows as "Untitled"), stashes
     * the turn, and navigates to the real conversation. Opening that route remounts the view with a fresh
     * context that replays the stashed turn — so the turn streams through the normal path, not this
     * about-to-be-discarded draft context.
     */
    async #createFromDraftAndHandoff(content: string, contentParts?: UaiInputContent[]): Promise<void> {
        if (this.#creating) return;
        this.#creating = true;

        const selected = this.#selectedAgent.getValue();
        const agentIdOrAlias = selected && selected.id !== AUTO_AGENT.id ? selected.id : undefined;

        const { data } = await this.#conversationRepository.create({
            projectId: this.#draftProjectId,
            title: deriveConversationTitle(content) || undefined,
            agentIdOrAlias,
        });

        if (!data?.id) {
            // Create failed — stay in draft so the user can retry (matches the prior new-chat behaviour).
            this.#creating = false;
            return;
        }

        this.#isDraft = false;
        stashPendingFirstMessage(data.id, { content, contentParts });
        navigateToWorkspacePath(copilotWorkspaceConversationPath(data.id));
    }

    /**
     * On the first message of an untitled conversation, derives a title from it, persists it, and
     * signals the sidebar to refresh. Sets the local title synchronously so a rapid second send
     * doesn't re-title.
     */
    #maybeAutoTitle(content: string): void {
        const conversation = this.#conversation;
        if (!conversation || conversation.title?.trim()) return;
        const title = deriveConversationTitle(content);
        if (!title) return;

        this.#conversation = { ...conversation, title };
        // rename() dispatches a UaiEntityActionEvent on the shared bus → the sidebar refreshes.
        void this.#conversationRepository.rename(conversation, title);
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
