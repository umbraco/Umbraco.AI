import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

export interface AgentInfo {
  id: string;
  name: string;
  alias: string;
}

export class UmbCopilotContext extends UmbControllerBase {
  public readonly IS_COPILOT_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentId = new UmbStringState("");
  readonly agentId = this.#agentId.asObservable();

  #agentName = new UmbStringState("");
  readonly agentName = this.#agentName.asObservable();

  constructor(host: UmbControllerHost) {
    super(host);
    this.provideContext(UMB_COPILOT_CONTEXT, this);
  }

  hasAgent(): boolean {
    return !!this.#agentId.getValue();
  }

  setAgent(agentId: string, agentName: string) {
    this.#agentId.setValue(agentId);
    this.#agentName.setValue(agentName);
  }

  clearAgent() {
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
    console.log("toggle")
    this.#isOpen.setValue(!this.#isOpen.getValue());
  }
}

export const UMB_COPILOT_CONTEXT = new UmbContextToken<UmbCopilotContext>(
  "UmbCopilotContext",
  undefined,
  (context): context is UmbCopilotContext =>
    (context as UmbCopilotContext).IS_COPILOT_CONTEXT
);

export default UmbCopilotContext;