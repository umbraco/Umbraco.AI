import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import { UaiCopilotRunController } from "./services/copilot-run.controller.js";
import UaiHitlContext, { UAI_HITL_CONTEXT } from "./hitl.context.js";
import { UaiCopilotAgentRepository } from "./repository";
import { UaiCopilotAgentItem } from "./types.ts";

/**
 * Root Copilot context that wires together agent data, run lifecycle, and tool coordination.
 * Provides Umb contexts so sidebar/header/chat elements can stay declarative.
 */
export class UaiCopilotContext extends UmbControllerBase {
  public readonly IS_COPILOT_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentRepository: UaiCopilotAgentRepository;
  #runController: UaiCopilotRunController;
  #hitlContext: UaiHitlContext;

  #agents = new UmbArrayState<UaiCopilotAgentItem>([], (x) => x.id);
  #selectedAgent = new UmbBasicState<UaiCopilotAgentItem | undefined>(undefined);
  #agentsLoading = new UmbBooleanState(false);

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

  /**
   * Observable for HITL (human-in-the-loop) interrupt state.
   * Emits when an interrupt requires user response.
   */
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
    this.provideContext(UAI_COPILOT_RUN_CONTEXT, this.#runController);
    this.provideContext(UAI_HITL_CONTEXT, this.#hitlContext);
  }

  /**
   * Load available agents from the server.
   */
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

  /**
   * Check if an agent is currently selected.
   */
  hasAgent(): boolean {
    return !!this.#selectedAgent.getValue();
  }

  /**
   * Get the currently selected agent ID.
   */
  getAgentId(): string | undefined {
    return this.#selectedAgent.getValue()?.id;
  }

  /**
   * Get the currently selected agent name.
   */
  getAgentName(): string | undefined {
    return this.#selectedAgent.getValue()?.name;
  }

  /**
   * Select an agent by its ID.
   * @param agentId The unique identifier of the agent to select
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

  /** Open the copilot panel. */
  open() {
    this.#isOpen.setValue(true);
  }

  /** Close the copilot panel. */
  close() {
    this.#isOpen.setValue(false);
  }

  /** Toggle the copilot panel visibility. */
  toggle() {
    this.#isOpen.setValue(!this.#isOpen.getValue());
  }

  /**
   * Respond to a HITL interrupt.
   * @param response The user's response to the interrupt
   */
  respondToHitl(response: string): void {
    this.#hitlContext.respond(response);
  }
}

/** Context token for the root Copilot context (agent selection, panel state). */
export const UAI_COPILOT_CONTEXT = new UmbContextToken<UaiCopilotContext>(
  "UaiCopilotContext",
  undefined,
  (context): context is UaiCopilotContext =>
    (context as UaiCopilotContext).IS_COPILOT_CONTEXT
);

/** Context token for the run controller (messages, streaming, lifecycle). */
export const UAI_COPILOT_RUN_CONTEXT = new UmbContextToken<UaiCopilotRunController>(
  "UaiCopilotRunContext"
);

export default UaiCopilotContext;
