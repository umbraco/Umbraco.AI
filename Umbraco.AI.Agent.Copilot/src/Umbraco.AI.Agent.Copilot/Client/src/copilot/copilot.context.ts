import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import {
    type Observable,
    map,
    distinctUntilChanged,
} from "@umbraco-cms/backoffice/external/rxjs";
import { debouncedHide } from "./utils/debounced-hide.js";
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
import { UaiCopilotHistoryStore } from "./services/copilot-history.store.js";
import { UaiCopilotLocalHistoryStrategy } from "./services/copilot-local-history.strategy.js";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "./contexts/entity-adapter.context-token.js";

/** A detected-entity key that has not been saved yet (no real unique) ends with this sentinel. */
const NEW_ENTITY_KEY_SUFFIX = ":new";

/** Whether a detected-entity key maps to a persistable thread (a saved item with a real unique). */
function isPersistableEntityKey(key: string | undefined): key is string {
    return !!key && !key.endsWith(NEW_ENTITY_KEY_SUFFIX);
}

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

    // ─── Per-node chat history ──────────────────────────────────────────────────
    #historyStore = new UaiCopilotHistoryStore();
    /** Storage key the in-memory conversation belongs to; undefined for an unsaved-new item. */
    #activeHistoryKey?: string;
    /** The detected-entity key the conversation is currently bound to (incl. ":new"). */
    #boundEntityKey?: string;
    /** Latest selected entity key, captured even before history binding starts. */
    #pendingEntityKey?: string;
    /** History binding defers until the first agent is set (so the first load runs on a ready controller). */
    #historyBound = false;

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

        // "Supported" == copilot has detected at least one entity it can act on. Hold the hide edge
        // (see debouncedHide) so supported⇄supported hops don't flip it to false.
        const supported$ = this.#entityAdapterContext.detectedEntities$.pipe(
            map((entities) => entities.length > 0),
            distinctUntilChanged(),
        );
        this.isSupportedWorkspace$ = debouncedHide(supported$, SUPPORT_HIDE_DELAY_MS);

        this.#runController = new UaiRunController(host, this.#hitlContext, {
            toolRendererManager: this.#_toolRendererManager,
            frontendToolManager,
            interruptHandlers: [
                new UaiHitlInterruptHandler(this),
                new UaiDefaultInterruptHandler(),
            ],
            // Client-owned conversation that also persists per node in localStorage. The strategy
            // loads the active node's thread (loadInitial) and saves after each turn (onTurnComplete),
            // keyed by whichever node is currently bound (see #activeHistoryKey / #handleEntitySelection).
            conversationStrategy: new UaiCopilotLocalHistoryStrategy(
                this.#historyStore,
                () => this.#activeHistoryKey,
                () => this.#selectedAgent.getValue()?.id,
            ),
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
                // Prefer the agent the user last chose over the default, so a reload doesn't quietly
                // put them back on "Auto". Falls through to the default if it's since been removed.
                const remembered = this.#historyStore.getLastAgentId();
                this.#selectedAgent.setValue(
                    displayAgents.find((a) => a.id === remembered) ?? displayAgents[0],
                );
            }

            const currentSelected = this.#selectedAgent.getValue();
            if (currentSelected && !displayAgents.find((a) => a.id === currentSelected.id)) {
                this.#selectedAgent.setValue(undefined);
            }
        });

        this.observe(this.selectedAgent, (agent) => {
            if (agent) {
                this.#runController.setAgent(agent);
                // Do the first history bind once an agent exists, so restore runs against a ready
                // controller. (setAgent now preserves the conversation across agent switches, so
                // later switches won't disturb the restored thread.)
                if (!this.#historyBound) {
                    this.#historyBound = true;
                    this.#handleEntitySelection(this.#pendingEntityKey);
                }
            }
        });

        // Track the selected entity and (once bound) swap the conversation to that item's thread.
        this.observe(this.selectedEntity$, (entity) => {
            this.#pendingEntityKey = entity?.key;
            if (this.#historyBound) {
                this.#handleEntitySelection(entity?.key);
            }
        });

        this.#bindHistoryToSession();

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
            // Only a deliberate choice is remembered — restoring a thread's own agent goes through
            // #restoreAgentForKey, which must not overwrite the user's standing preference.
            this.#historyStore.rememberLastAgentId(agentId);
        }
    }

    // ─── Panel Actions ─────────────────────────────────────────────────────────

    open(): void {
        this.#isOpen.setValue(true);
    }

    close(): void {
        this.#isOpen.setValue(false);
        // Closing (or auto-closing when leaving a workspace) only aborts an in-flight run — it no
        // longer wipes the conversation. The thread stays in memory (and, for saved items, in local
        // history, saved after each turn by the conversation strategy) so re-opening restores where
        // the user left off.
        this.#runController.abortRun();
    }

    toggle(): void {
        const wasOpen = this.#isOpen.getValue();
        this.#isOpen.setValue(!wasOpen);
        if (wasOpen) {
            this.#runController.abortRun();
        }
    }

    /** Clears the current conversation and forgets its stored history. */
    clearChat(): void {
        this.#runController.abortRun();
        this.#runController.resetConversation();
        if (this.#activeHistoryKey) {
            this.#historyStore.remove(this.#activeHistoryKey);
        }
    }

    // ─── Session lifecycle (history retention) ──────────────────────────────────

    /**
     * Ties the local history store's lifetime to the backoffice session: an explicit sign-out
     * clears everything immediately (a deliberate action, no grace period), while a session timeout
     * is only forgiven if the user resumes on the same calendar day (see
     * {@link UaiCopilotHistoryStore.consumeTimeout}).
     *
     * Distinguishing the two relies on `UmbAuthContext.timeOut()`'s exact ordering: it sets
     * `isAuthorized` to `false` and *then* emits `timeoutSignal`, synchronously, in that order. So a
     * `isAuthorized`→`false` transition can't yet know whether a timeout signal is coming — we defer
     * the "was this a plain sign-out" check to a microtask, giving a same-tick `timeoutSignal`
     * emission a chance to flag it first. `signOut()` doesn't emit `timeoutSignal` at all, so an
     * unflagged transition is unambiguously an explicit sign-out.
     */
    #bindHistoryToSession(): void {
        let hasBeenAuthorized = false;
        let sawTimeoutForThisTransition = false;

        this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
            if (!authContext) return;

            this.observe(authContext.timeoutSignal, () => {
                sawTimeoutForThisTransition = true;
                this.#historyStore.recordTimeout();
            });

            this.observe(authContext.isAuthorized, (isAuthorized) => {
                if (isAuthorized) {
                    hasBeenAuthorized = true;
                    this.#historyStore.consumeTimeout();
                    return;
                }

                // Ignore the initial emission when the app loads while already signed out — there's
                // no live sign-out/timeout to react to, and clearing here would wipe yesterday's
                // history before the user even gets a chance to log back in and let the same-day
                // check above run.
                if (!hasBeenAuthorized) return;

                sawTimeoutForThisTransition = false;
                queueMicrotask(() => {
                    if (!sawTimeoutForThisTransition) {
                        this.#historyStore.clearAll();
                    }
                });
            });
        });
    }

    // ─── Per-node history plumbing ──────────────────────────────────────────────

    /**
     * Binds the conversation to the newly-selected entity's thread.
     *
     * The copilot can only observe that the selected entity's key changed; it cannot tell a "new item
     * was just saved" (carry the chat over) apart from a "navigated to a different item" (switch
     * threads), because both surface as `type:new` → `type:{guid}`. We resolve that with a heuristic:
     * an unsaved-new conversation that has messages, moving to a persistable key that has no stored
     * history, is treated as a save and carried over. Otherwise it's a switch: the conversation
     * strategy reloads the incoming node's thread (the outgoing one was already saved after its last
     * turn by the strategy's onTurnComplete). Consequence: abandoning an unsaved new item by
     * navigating away loses its chat (accepted), and directly navigating from an unsaved new item to a
     * history-less existing item would carry the chat over (rare, low-harm).
     *
     * Persistence itself lives in {@link UaiCopilotLocalHistoryStrategy}, which reads the active key
     * via the accessor passed at construction — so setting #activeHistoryKey here is what steers which
     * node the strategy loads/saves.
     */
    #handleEntitySelection(newKey: string | undefined): void {
        if (newKey === this.#boundEntityKey) return;

        const prevActiveStorageKey = this.#activeHistoryKey;
        const hadMessages = this.#runController.messages.length > 0;

        // Leaving to "no entity" (e.g. an unsupported workspace): keep the conversation as-is so a
        // later re-open restores it. Don't rebind yet.
        if (newKey === undefined) {
            this.#boundEntityKey = undefined;
            return;
        }

        const newStorageKey = isPersistableEntityKey(newKey) ? newKey : undefined;

        // Save-rekey heuristic (see method doc): adopt the new key and persist the carried-over
        // conversation now — onTurnComplete won't fire again until the next turn, so the pre-save
        // turns would otherwise never reach storage under the real key.
        if (
            prevActiveStorageKey === undefined &&
            hadMessages &&
            newStorageKey &&
            !this.#historyStore.has(newStorageKey)
        ) {
            this.#activeHistoryKey = newStorageKey;
            this.#boundEntityKey = newKey;
            this.#historyStore.save(
                newStorageKey,
                this.#runController.messages,
                this.#selectedAgent.getValue()?.id,
            );
            return;
        }

        // Navigation/switch: rebind to the incoming key, then let the strategy load its thread. Abort
        // any in-flight run first so transient state doesn't leak across the swap (loadInitialMessages
        // only replaces the message list).
        this.#runController.abortRun();
        this.#activeHistoryKey = newStorageKey;
        this.#boundEntityKey = newKey;
        this.#restoreAgentForKey(newStorageKey);
        void this.#runController.loadInitialMessages();
    }

    /**
     * Puts the agent selector back on whichever agent the incoming item's thread was last run with,
     * so continuing an old conversation continues it with the agent that produced it. Items with no
     * stored thread keep the current selection (which itself starts from the user's last choice).
     */
    #restoreAgentForKey(storageKey: string | undefined): void {
        if (!storageKey) return;

        const storedAgentId = this.#historyStore.loadAgentId(storageKey);
        if (!storedAgentId || storedAgentId === this.#selectedAgent.getValue()?.id) return;

        const agent = this.#agents.getValue().find((a) => a.id === storedAgentId);
        if (agent) {
            this.#selectedAgent.setValue(agent);
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

    regenerateLastMessage(): Promise<void> {
        return this.#runController.regenerateLastMessage();
    }
}

export const UAI_COPILOT_CONTEXT = new UmbContextToken<UaiCopilotContext>(
    "UaiCopilotContext",
    undefined,
    (context): context is UaiCopilotContext => (context as UaiCopilotContext).IS_COPILOT_CONTEXT,
);

export default UaiCopilotContext;
