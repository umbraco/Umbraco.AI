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
import { ServerPersistedConversationStrategy } from "./server-persisted-conversation.strategy.js";
import { UaiWorkspaceAgentRepository } from "./workspace-agent.repository.js";
import { notifyCopilotWorkspaceConversationsChanged } from "../constants.js";

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
export class CopilotWorkspaceChatContext extends UmbControllerBase implements UaiChatContextApi {
    public readonly IS_COPILOT_WORKSPACE_CONTEXT = true;

    #conversationRepository: UaiConversationRepository;
    #agentRepository: UaiWorkspaceAgentRepository;
    #strategy: ServerPersistedConversationStrategy;
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    #toolRendererManager: UaiToolRendererManager;

    #agents = new UmbArrayState<UaiAgentItem>([], (x) => x.id);
    #selectedAgent = new UmbBasicState<UaiAgentItem | undefined>(undefined);
    #agentsLoading = new UmbBooleanState(false);

    #conversationId?: string;
    #conversation?: ConversationResponseModel;

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
        this.#strategy = new ServerPersistedConversationStrategy(this.#conversationRepository);
        const frontendToolManager = new UaiFrontendToolManager(host);

        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager: this.#toolRendererManager,
            frontendToolManager,
            conversationStrategy: this.#strategy,
            interruptHandlers: [new UaiHitlInterruptHandler(this), new UaiDefaultInterruptHandler()],
        });

        // Maintain the picker's agent list (with an "Auto" option when >1 agent), keeping the
        // current selection valid as the catalog loads/changes.
        this.observe(this.#agentRepository.agentItems$, (agents) => {
            const displayAgents = agents.length > 1 ? [AUTO_AGENT, ...agents] : [...agents];
            this.#agents.setValue(displayAgents);

            const selected = this.#selectedAgent.getValue();
            if (selected && !displayAgents.find((a) => a.id === selected.id)) {
                this.#selectedAgent.setValue(this.#resolveSelectedAgent(displayAgents));
            }
        });

        this.provideContext(UAI_CHAT_CONTEXT, this);
        this.provideContext(UAI_HITL_CONTEXT, this.#hitlContext);
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

        this.#runController.abortRun();
        this.#strategy.setConversationId(conversationId);

        // A synthetic per-conversation agent id makes setAgent recreate the (conversation-keyed)
        // client and reset the thread; the real agent is resolved server-side.
        this.#runController.setAgent({ id: `conversation:${conversationId}`, name: "Workspace", alias: "workspace" });

        const { data } = await this.#conversationRepository.requestById(conversationId);
        this.#conversation = data ?? undefined;
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
        void this.#conversationRepository.update(conversation.id, {
            title: conversation.title ?? null,
            projectId: conversation.projectId ?? null,
            agentIdOrAlias,
            profileId: conversation.profileId ?? null,
            isPinned: conversation.isPinned,
            isArchived: conversation.isArchived,
        });
    }

    respondToHitl(response: string): void {
        this.#hitlContext.respond(response);
    }

    async sendUserMessage(content: string, contentParts?: UaiInputContent[]): Promise<void> {
        // Title an untitled conversation from its first message (fire-and-forget; doesn't delay the send).
        this.#maybeAutoTitle(content);
        // Project context is injected server-side from the conversation; no client context needed.
        this.#runController.sendUserMessage(content, [], contentParts);
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
        void this.#conversationRepository.rename(conversation, title).then((result) => {
            if (!result.error) notifyCopilotWorkspaceConversationsChanged();
        });
    }

    abortRun(): void {
        this.#runController.abortRun();
    }

    regenerateLastMessage(): void {
        this.#runController.regenerateLastMessage();
    }
}

export const UAI_COPILOT_WORKSPACE_CHAT_CONTEXT = new UmbContextToken<CopilotWorkspaceChatContext>(
    "UaiCopilotWorkspaceChatContext",
    undefined,
    (context): context is CopilotWorkspaceChatContext =>
        (context as CopilotWorkspaceChatContext).IS_COPILOT_WORKSPACE_CONTEXT,
);

export default CopilotWorkspaceChatContext;
