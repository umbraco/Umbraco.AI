import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
import { CopilotAgentStore } from "./stores/copilot-agent.store.js";
import { CopilotRunController } from "./controllers/copilot-run.controller.js";
import { CopilotToolBus } from "./services/copilot-tool-bus.js";
import type { CopilotAgentItem } from "../ui/sidebar/copilot.repository.js";

/**
 * Basic agent information used for display purposes.
 */
export interface AgentInfo {
  id: string;
  name: string;
  alias: string;
}

/**
 * Root Copilot context that wires together agent data, run lifecycle, and tool coordination.
 * Provides Umb contexts so sidebar/header/chat elements can stay declarative.
 */
export class UmbCopilotContext extends UmbControllerBase {
  public readonly IS_COPILOT_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentId = new UmbStringState("");
  readonly agentId = this.#agentId.asObservable();

  #agentName = new UmbStringState("");
  readonly agentName = this.#agentName.asObservable();

  #agentStore: CopilotAgentStore;
  #toolBus: CopilotToolBus;
  #runController: CopilotRunController;

  readonly agents: Observable<CopilotAgentItem[]>;
  readonly selectedAgent: Observable<CopilotAgentItem | undefined>;
  readonly agentsLoading: Observable<boolean>;

  get messages$() {
    return this.#runController.messages$;
  }

  get streamingContent$() {
    return this.#runController.streamingContent$;
  }

  get agentState$() {
    return this.#runController.agentState$;
  }

  get interrupt$() {
    return this.#runController.interrupt$;
  }

  get runStatus$() {
    return this.#runController.runStatus$;
  }

  constructor(host: UmbControllerHost) {
    super(host);

    this.#toolBus = new CopilotToolBus(host);
    this.#runController = new CopilotRunController(host, this.#toolBus);
    this.#agentStore = new CopilotAgentStore(host);

    this.agents = this.#agentStore.agents$;
    this.selectedAgent = this.#agentStore.selectedAgent$;
    this.agentsLoading = this.#agentStore.loading$;

    this.observe(this.#agentStore.selectedAgent$, (agent) => {
      if (agent) {
        this.#agentId.setValue(agent.id);
        this.#agentName.setValue(agent.name);
        this.#runController.setAgent(agent);
      } else {
        this.#agentId.setValue("");
        this.#agentName.setValue("");
      }
    });

    this.provideContext(UMB_COPILOT_CONTEXT, this);
    this.provideContext(UMB_COPILOT_RUN_CONTEXT, this.#runController);
    this.provideContext(UMB_COPILOT_TOOL_BUS_CONTEXT, this.#toolBus);
  }

  /**
   * Load available agents from the server.
   */
  loadAgents(): Promise<void> {
    return this.#agentStore.loadAgents();
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
  setAgent(agentId: string): void {
    this.#agentStore.selectAgentById(agentId);
  }

  /**
   * Clear the currently selected agent.
   * Available for external use when deselecting agents programmatically.
   */
  clearAgent(): void {
    this.#agentId.setValue("");
    this.#agentName.setValue("");
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
}

/** Context token for the root Copilot context (agent selection, panel state). */
export const UMB_COPILOT_CONTEXT = new UmbContextToken<UmbCopilotContext>(
  "UmbCopilotContext",
  undefined,
  (context): context is UmbCopilotContext =>
    (context as UmbCopilotContext).IS_COPILOT_CONTEXT
);

/** Context token for the run controller (messages, streaming, lifecycle). */
export const UMB_COPILOT_RUN_CONTEXT = new UmbContextToken<CopilotRunController>(
  "UmbCopilotRunContext"
);

/** Context token for the tool bus (frontend tool execution coordination). */
export const UMB_COPILOT_TOOL_BUS_CONTEXT = new UmbContextToken<CopilotToolBus>(
  "UmbCopilotToolBusContext"
);

export default UmbCopilotContext;
