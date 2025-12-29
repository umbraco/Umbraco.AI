import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbArrayState, UmbBasicState, UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
import { UaiCopilotRunController } from "./services/copilot-run.controller.js";
import UaiHitlContext, { UAI_HITL_CONTEXT } from "./hitl.context.js";
import { UaiCopilotAgentRepository } from "./repository";
import { UaiCopilotAgentItem } from "./types.ts";

/**
 * Basic agent information used for display purposes.
 */
export interface UaiAgentInfo {
  id: string;
  name: string;
  alias: string;
}

/**
 * Root Copilot context that wires together agent data, run lifecycle, and tool coordination.
 * Provides Umb contexts so sidebar/header/chat elements can stay declarative.
 */
export class UaiCopilotContext extends UmbControllerBase {
  public readonly IS_COPILOT_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentId = new UmbStringState("");
  readonly agentId = this.#agentId.asObservable();

  #agentName = new UmbStringState("");
  readonly agentName = this.#agentName.asObservable();

  #agentRepository: UaiCopilotAgentRepository;
  #runController: UaiCopilotRunController;
  #hitlContext: UaiHitlContext;

  #agents: UmbArrayState<UaiCopilotAgentItem> = new UmbArrayState<UaiCopilotAgentItem>([], (x) => x.id);
  #selectedAgent: UmbBasicState<UaiCopilotAgentItem | undefined> = new UmbBasicState<UaiCopilotAgentItem | undefined>(undefined);
  #agentsLoading: UmbBasicState<boolean> = new UmbBasicState<boolean>(false);
  
  readonly agents: Observable<UaiCopilotAgentItem[]> = this.#agents.asObservable();
  readonly selectedAgent: Observable<UaiCopilotAgentItem | undefined> = this.#selectedAgent.asObservable();
  readonly agentsLoading: Observable<boolean> = this.#agentsLoading.asObservable();

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

    this.observe(this.selectedAgent, (agent) => {
      if (agent) {
        this.#agentId.setValue(agent.id);
        this.#agentName.setValue(agent.name);
        this.#runController.setAgent(agent);
      } else {
        this.#agentId.setValue("");
        this.#agentName.setValue("");
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
    
    // Set loading state
    this.#agentsLoading.setValue(true);
    
    // Load and observe selected agent
    const { asObservable: agentsObservable } = await this.#agentRepository.getOrFetchAll();
    if (!agentsObservable) return;
    this.observe(agentsObservable(), (agents) => {
      this.#agents.setValue(agents);
    });
    
    // Load and observe selected agent
    const { asObservable: selectedAsObservable } = await this.#agentRepository.selected();
    if (!selectedAsObservable) return;
    this.observe(selectedAsObservable(), (agent) => {
      this.#selectedAgent.setValue(agent);
    });
    
    // Clear loading state
    this.#agentsLoading.setValue(false);
    
  }

  /**
   * Check if an agent is currently selected.
   */
  hasAgent(): boolean {
    return !!this.#agentId.getValue();
  }

  /**
   * Select an agent by its ID.
   * @param agentId The unique identifier of the agent to select
   */
  async setAgent(agentId: string): Promise<void> {
    await this.#agentRepository.select(agentId);
  }

  /**
   * Clear the currently selected agent.
   * Available for external use when deselecting agents programmatically.
   */
  async clearAgent(): Promise<void> {
    await this.#agentRepository.select(undefined);
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
