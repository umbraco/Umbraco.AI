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
    UAI_ENTITY_CONTEXT,
    UaiHitlInterruptHandler,
    UaiDefaultInterruptHandler,
    type UaiChatContextApi,
    type UaiAgentItem,
} from "@umbraco-ai/agent-ui";
import { UaiCopilotAgentRepository } from "./repository";
import { UaiEntityAdapterContext, type UaiValueChange, type UaiValueChangeResult } from "@umbraco-ai/core";
import { UaiCopilotEntityContext } from "./services/copilot-entity.context.js";
import { getSectionPathnameFromUrl } from "./context-observer.js";

/**
 * Facade context providing a unified API for all Copilot functionality.
 *
 * Implements UaiChatContextApi so shared chat components can consume it via UAI_CHAT_CONTEXT.
 * Also provides copilot-specific features (panel state, entity context) via UAI_COPILOT_CONTEXT.
 */
export class UaiCopilotContext extends UmbControllerBase implements UaiChatContextApi {
    /** Type guard marker for context resolution. */
    public readonly IS_COPILOT_CONTEXT = true;

    #isOpen = new UmbBooleanState(false);
    #agentRepository: UaiCopilotAgentRepository;
    #runController: UaiRunController;
    #hitlContext: UaiHitlContext;
    #entityAdapterContext: UaiEntityAdapterContext;
    #entityContext: UaiCopilotEntityContext;
    #_toolRendererManager: UaiToolRendererManager;
    #agents = new UmbArrayState<UaiAgentItem>([], (x) => x.id);
    #selectedAgent = new UmbBasicState<UaiAgentItem | undefined>(undefined);
    #agentsLoading = new UmbBooleanState(false);

    // ─── Panel State ───────────────────────────────────────────────────────────

    readonly isOpen = this.#isOpen.asObservable();

    // ─── Agent Catalog ─────────────────────────────────────────────────────────

    readonly agents = this.#agents.asObservable();
    readonly selectedAgent = this.#selectedAgent.asObservable();
    readonly agentsLoading = this.#agentsLoading.asObservable();

    // ─── Run State (delegated to RunController from agent-ui) ──────────────────

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

    // ─── Tool Management ───────────────────────────────────────────────────────

    get toolRendererManager(): UaiToolRendererManager {
        return this.#_toolRendererManager;
    }

    // ─── HITL (Human-in-the-Loop) ──────────────────────────────────────────────

    get hitlInterrupt$() {
        return this.#hitlContext.interrupt$;
    }

    get pendingApproval$() {
        return this.#hitlContext.pendingApproval$;
    }

    // ─── Entity Context (copilot-specific) ──────────────────────────────────────

    get detectedEntities$() {
        return this.#entityAdapterContext.detectedEntities$;
    }

    get selectedEntity$() {
        return this.#entityAdapterContext.selectedEntity$;
    }

    setSelectedEntityKey(key: string | undefined): void {
        this.#entityAdapterContext.setSelectedEntityKey(key);
    }

    async applyValueChange(change: UaiValueChange): Promise<UaiValueChangeResult> {
        return this.#entityAdapterContext.applyValueChange(change);
    }

    constructor(host: UmbControllerHost) {
        super(host);

        this.#agentRepository = new UaiCopilotAgentRepository(host);
        this.#hitlContext = new UaiHitlContext(host);
        this.#_toolRendererManager = new UaiToolRendererManager(host);
        const frontendToolManager = new UaiFrontendToolManager(host);
        this.#entityAdapterContext = new UaiEntityAdapterContext(host);
        this.#entityContext = new UaiCopilotEntityContext(host, this.#entityAdapterContext);

        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager: this.#_toolRendererManager,
            frontendToolManager,
            interruptHandlers: [
                new UaiHitlInterruptHandler(this),
                new UaiDefaultInterruptHandler(),
            ],
        });

        this.observe(this.#agentRepository.agentItems$, (agents) => {
            let displayAgents = [...agents];

            // Add "Auto" option when multiple agents available
            if (agents.length > 1) {
                displayAgents = [
                    { id: "auto", name: "Auto", alias: "auto" },
                    ...agents,
                ];
            }

            this.#agents.setValue(displayAgents);

            // Default to "Auto" when multiple agents, first agent otherwise
            if (!this.#selectedAgent.getValue() && displayAgents.length > 0) {
                this.#selectedAgent.setValue(displayAgents[0]);
            }

            const currentSelected = this.#selectedAgent.getValue();
            if (currentSelected && !displayAgents.find((a) => a.id === currentSelected.id)) {
                this.#selectedAgent.setValue(undefined);
            }
        });

        this.observe(this.selectedAgent, (agent) => {
            if (agent) {
                this.#runController.setAgent(agent);
            }
        });

        this.provideContext(UAI_COPILOT_CONTEXT, this);
        this.provideContext(UAI_CHAT_CONTEXT, this);
        this.provideContext(UAI_HITL_CONTEXT, this.#hitlContext);
        this.provideContext(UAI_ENTITY_CONTEXT, this.#entityContext);
    }

    // ─── Agent Catalog Actions ─────────────────────────────────────────────────

    async loadAgents(): Promise<void> {
        this.#agentsLoading.setValue(true);
        await this.#agentRepository.initialize();
        this.#agentsLoading.setValue(false);
    }

    hasAgent(): boolean {
        return !!this.#selectedAgent.getValue();
    }

    getAgentId(): string | undefined {
        return this.#selectedAgent.getValue()?.id;
    }

    getAgentName(): string | undefined {
        return this.#selectedAgent.getValue()?.name;
    }

    selectAgent(agentId: string | undefined): void {
        if (!agentId) {
            this.#selectedAgent.setValue(undefined);
            return;
        }
        const agent = this.#agents.getValue().find((a) => a.id === agentId);
        if (agent) {
            this.#selectedAgent.setValue(agent);
        }
    }

    // ─── Panel Actions ─────────────────────────────────────────────────────────

    open(): void {
        this.#isOpen.setValue(true);
    }

    close(): void {
        this.#isOpen.setValue(false);
        this.#runController.abortRun();
        this.#runController.resetConversation();
    }

    toggle(): void {
        const wasOpen = this.#isOpen.getValue();
        this.#isOpen.setValue(!wasOpen);
        if (wasOpen) {
            this.#runController.abortRun();
            this.#runController.resetConversation();
        }
    }

    // ─── HITL Actions ──────────────────────────────────────────────────────────

    respondToHitl(response: string): void {
        this.#hitlContext.respond(response);
    }

    // ─── Run Actions ───────────────────────────────────────────────────────────

    async sendUserMessage(content: string): Promise<void> {
        const entityContext = await this.#entityAdapterContext.serializeSelectedEntity();

        const context: Array<{ description: string; value: string }> = [];

        // Add section context
        const currentSection = getSectionPathnameFromUrl();
        if (currentSection) {
            context.push({
                description: `Current section: ${currentSection}`,
                value: JSON.stringify({ section: currentSection }),
            });
        }

        // Add entity context
        if (entityContext) {
            context.push({
                description: `Currently editing ${entityContext.entityType}: ${entityContext.name}`,
                value: JSON.stringify(entityContext),
            });
        }

        this.#runController.sendUserMessage(content, context);
    }

    abortRun(): void {
        this.#runController.abortRun();
    }

    regenerateLastMessage(): void {
        this.#runController.regenerateLastMessage();
    }
}

export const UAI_COPILOT_CONTEXT = new UmbContextToken<UaiCopilotContext>(
    "UaiCopilotContext",
    undefined,
    (context): context is UaiCopilotContext => (context as UaiCopilotContext).IS_COPILOT_CONTEXT,
);

export default UaiCopilotContext;
