import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
import { CopilotAgentStore } from "./stores/copilot-agent.store.js";
import { CopilotRunController } from "./controllers/copilot-run.controller.js";
import { CopilotToolBus } from "./services/copilot-tool-bus.js";
import type { CopilotAgentItem } from "../ui/sidebar/copilot.repository.js";

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

  loadAgents(): Promise<void> {
    return this.#agentStore.loadAgents();
  }

  hasAgent(): boolean {
    return !!this.#agentId.getValue();
  }

  setAgent(agentId: string): void {
    this.#agentStore.selectAgentById(agentId);
  }

  clearAgent(): void {
    this.#agentId.setValue("");
    this.#agentName.setValue("");
  }

  open() {
    this.#isOpen.setValue(true);
  }

  close() {
    this.#isOpen.setValue(false);
  }

  toggle() {
    this.#isOpen.setValue(!this.#isOpen.getValue());
  }
}

export const UMB_COPILOT_CONTEXT = new UmbContextToken<UmbCopilotContext>(
  "UmbCopilotContext",
  undefined,
  (context): context is UmbCopilotContext =>
    (context as UmbCopilotContext).IS_COPILOT_CONTEXT
);

export const UMB_COPILOT_RUN_CONTEXT = new UmbContextToken<CopilotRunController>(
  "UmbCopilotRunContext"
);

export const UMB_COPILOT_TOOL_BUS_CONTEXT = new UmbContextToken<CopilotToolBus>(
  "UmbCopilotToolBusContext"
);

export default UmbCopilotContext;
