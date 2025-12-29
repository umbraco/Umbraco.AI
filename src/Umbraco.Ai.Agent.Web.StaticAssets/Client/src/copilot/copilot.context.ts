import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UaiCopilotRunController } from "./services/copilot-run.controller.js";
import UaiHitlContext, { UAI_HITL_CONTEXT } from "./hitl.context.js";
import { UaiCopilotAgentRepository } from "./repository";
import { UaiCopilotAgentItem } from "./types.ts";
import type { UaiFrontendToolManager } from "./services/frontend-tool.manager.ts";

/**
 * Facade context providing a unified API for all Copilot functionality.
 *
 * This context acts as the single entry point for UI components, abstracting away
 * the internal separation between agent management, run lifecycle, and tool coordination.
 * Components should consume only `UAI_COPILOT_CONTEXT` rather than accessing internal
 * controllers directly.
 *
 * ## Responsibilities
 * - **Panel state**: Controls sidebar visibility (open/close/toggle)
 * - **Agent catalog**: Loads and exposes available agents from the server
 * - **Agent selection**: Manages which agent is currently active
 * - **Run state**: Exposes message history, streaming content, and execution state
 * - **Run actions**: Provides methods to send messages, abort, and regenerate
 * - **Tool management**: Exposes the frontend tool manager for custom tool rendering
 * - **HITL interrupts**: Handles human-in-the-loop approval flows
 */
export class UaiCopilotContext extends UmbControllerBase {
  /** Type guard marker for context resolution. */
  public readonly IS_COPILOT_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  #agentRepository: UaiCopilotAgentRepository;
  #runController: UaiCopilotRunController;
  #hitlContext: UaiHitlContext;
  #agents = new UmbArrayState<UaiCopilotAgentItem>([], (x) => x.id);
  #selectedAgent = new UmbBasicState<UaiCopilotAgentItem | undefined>(undefined);
  #agentsLoading = new UmbBooleanState(false);

  // ─── Panel State ───────────────────────────────────────────────────────────

  /** Observable for panel open/closed state. */
  readonly isOpen = this.#isOpen.asObservable();

  // ─── Agent Catalog ─────────────────────────────────────────────────────────

  /** Observable list of available agents. */
  readonly agents = this.#agents.asObservable();

  /** Observable for the currently selected agent. */
  readonly selectedAgent = this.#selectedAgent.asObservable();

  /** Observable indicating whether agents are being loaded. */
  readonly agentsLoading = this.#agentsLoading.asObservable();

  // ─── Run State (delegated to RunController) ────────────────────────────────

  /** Observable list of chat messages in the current conversation. */
  get messages$() {
    return this.#runController.messages$;
  }

  /** Observable for streaming text content during assistant response. */
  get streamingContent$() {
    return this.#runController.streamingContent$;
  }

  /** Observable for the current agent execution state (thinking, executing, etc.). */
  get agentState$() {
    return this.#runController.agentState$;
  }

  /** Observable indicating whether an agent run is in progress. */
  get isRunning$() {
    return this.#runController.isRunning$;
  }

  // ─── Tool Management ───────────────────────────────────────────────────────

  /** Frontend tool manager for registering and rendering custom tool UI. */
  get toolManager(): UaiFrontendToolManager {
    return this.#runController.toolManager;
  }

  // ─── HITL (Human-in-the-Loop) ──────────────────────────────────────────────

  /** Observable for HITL interrupt state. Emits when user approval is required. */
  get hitlInterrupt$() {
    return this.#hitlContext.interrupt$;
  }

  constructor(host: UmbControllerHost) {
    super(host);

    this.#agentRepository = new UaiCopilotAgentRepository(host);
    this.#hitlContext = new UaiHitlContext(host);
    this.#runController = new UaiCopilotRunController(host, this.#hitlContext);

    // Sync selected agent to run controller
    this.observe(this.selectedAgent, (agent) => {
      if (agent) {
        this.#runController.setAgent(agent);
      }
    });

    this.provideContext(UAI_COPILOT_CONTEXT, this);
    this.provideContext(UAI_HITL_CONTEXT, this.#hitlContext);
  }

  // ─── Agent Catalog Actions ─────────────────────────────────────────────────

  /** Load available agents from the server. Auto-selects first agent if none selected. */
  async loadAgents(): Promise<void> {
    this.#agentsLoading.setValue(true);

    const { data } = await this.#agentRepository.fetchActiveAgents();

    if (data) {
      this.#agents.setValue(data);

      // Auto-select first agent if none selected
      if (!this.#selectedAgent.getValue() && data.length > 0) {
        this.#selectedAgent.setValue(data[0]);
      }
    }

    this.#agentsLoading.setValue(false);
  }

  /** Check if an agent is currently selected. */
  hasAgent(): boolean {
    return !!this.#selectedAgent.getValue();
  }

  /** Get the currently selected agent ID. */
  getAgentId(): string | undefined {
    return this.#selectedAgent.getValue()?.id;
  }

  /** Get the currently selected agent name. */
  getAgentName(): string | undefined {
    return this.#selectedAgent.getValue()?.name;
  }

  /**
   * Select an agent by its ID. Clears selection if agentId is undefined.
   * @param agentId The unique identifier of the agent to select, or undefined to clear
   */
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

  /** Open the copilot panel. */
  open(): void {
    this.#isOpen.setValue(true);
  }

  /** Close the copilot panel. */
  close(): void {
    this.#isOpen.setValue(false);
  }

  /** Toggle the copilot panel visibility. */
  toggle(): void {
    this.#isOpen.setValue(!this.#isOpen.getValue());
  }

  // ─── HITL Actions ──────────────────────────────────────────────────────────

  /**
   * Respond to a HITL interrupt with user input.
   * @param response The user's response to the interrupt prompt
   */
  respondToHitl(response: string): void {
    this.#hitlContext.respond(response);
  }

  // ─── Run Actions ───────────────────────────────────────────────────────────

  /**
   * Send a user message to the agent, starting or continuing a conversation.
   * @param content The message content to send
   */
  sendUserMessage(content: string): void {
    this.#runController.sendUserMessage(content);
  }

  /** Abort the current agent run. Cancels any in-flight execution. */
  abortRun(): void {
    this.#runController.abortRun();
  }

  /** Regenerate the last assistant message by removing it and re-running. */
  regenerateLastMessage(): void {
    this.#runController.regenerateLastMessage();
  }
}

/**
 * Context token for consuming the Copilot facade.
 * This is the primary context that UI components should consume.
 */
export const UAI_COPILOT_CONTEXT = new UmbContextToken<UaiCopilotContext>(
  "UaiCopilotContext",
  undefined,
  (context): context is UaiCopilotContext =>
    (context as UaiCopilotContext).IS_COPILOT_CONTEXT
);

export default UaiCopilotContext;
