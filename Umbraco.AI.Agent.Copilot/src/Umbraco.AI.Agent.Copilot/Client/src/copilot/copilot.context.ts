import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import {
    Observable,
    map,
    distinctUntilChanged,
    shareReplay,
} from "@umbraco-cms/backoffice/external/rxjs";
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
import {
    UaiEntityAdapterContext,
    UaiRequestContextCollector,
    type UaiValueChange,
    type UaiValueChangeResult,
} from "@umbraco-ai/core";
import { UaiCopilotEntityContext } from "./services/copilot-entity.context.js";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "./contexts/entity-adapter.context-token.js";

// When leaving a supported workspace, "supported" flips to false only after this delay. The show
// edge is immediate. Moving between two supported workspaces briefly empties the detected-entity
// list as one workspace tears down before the next registers; this window lets the new detection
// arrive and cancel the pending flip, so both the FAB and the sidebar stay put rather than
// flickering (FAB) or auto-closing and wiping the conversation (sidebar).
const SUPPORT_HIDE_DELAY_MS = 200;

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
    #requestContextCollector: UaiRequestContextCollector;
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

    /**
     * Whether the current workspace(s) are supported by copilot — i.e. copilot has detected at least
     * one entity it can act on. Derived from `detectedEntities$` with a debounced hide edge (see
     * SUPPORT_HIDE_DELAY_MS). Both the FAB (visibility) and the sidebar (auto-close) consume this so
     * they share one rule and one debounce. Assigned in the constructor once the entity adapter
     * context exists.
     */
    readonly isSupportedWorkspace$: Observable<boolean>;

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
        this.#requestContextCollector = new UaiRequestContextCollector(host);

        // Emit `true` immediately when a workspace becomes supported, but delay the flip to `false`
        // by SUPPORT_HIDE_DELAY_MS. A pending hide is cancelled if support returns within the window,
        // so supported⇄supported hops (which briefly empty the detection list) never emit `false`.
        const supported$ = this.#entityAdapterContext.detectedEntities$.pipe(
            map((entities) => entities.length > 0),
            distinctUntilChanged(),
        );
        this.isSupportedWorkspace$ = new Observable<boolean>((subscriber) => {
            let hideTimer = 0;
            let current: boolean | undefined;
            const emit = (value: boolean) => {
                if (current !== value) {
                    current = value;
                    subscriber.next(value);
                }
            };
            const sub = supported$.subscribe((supported) => {
                window.clearTimeout(hideTimer);
                if (supported) {
                    emit(true);
                } else {
                    hideTimer = window.setTimeout(() => emit(false), SUPPORT_HIDE_DELAY_MS);
                }
            });
            return () => {
                window.clearTimeout(hideTimer);
                sub.unsubscribe();
            };
        }).pipe(shareReplay(1));

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

            // Add "Auto" option only when multiple agents are available
            if (agents.length > 1) {
                displayAgents = [
                    { id: "auto", name: "Auto", alias: "auto" },
                    ...agents,
                ];
            }

            this.#agents.setValue(displayAgents);

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
        // Provide the rich entity adapter context so tools (e.g. property value operation tools)
        // can read full envelopes and apply value changes through the same staging path the user's
        // typing uses. The shared `UAI_ENTITY_CONTEXT` (above) stays as a thin facade for
        // surfaces in `Umbraco.AI.Agent.UI` that don't need the structured operations.
        this.provideContext(UAI_ENTITY_ADAPTER_CONTEXT, this.#entityAdapterContext);
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

    async sendUserMessage(content: string, contentParts?: import("@umbraco-ai/agent-ui").UaiInputContent[]): Promise<void> {
        const items = await this.#requestContextCollector.collect();
        const context = items.map((item) => ({
            description: item.description,
            value: item.value ?? "",
        }));
        this.#runController.sendUserMessage(content, context, contentParts);
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
