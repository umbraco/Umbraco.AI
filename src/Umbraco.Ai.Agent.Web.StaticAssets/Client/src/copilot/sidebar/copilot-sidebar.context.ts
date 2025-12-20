import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

export interface AgentInfo {
  id: string;
  name: string;
  alias: string;
}

export class UmbCopilotSidebarContext extends UmbControllerBase {
  public readonly IS_COPILOT_SIDEBAR_CONTEXT = true;

  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentId = new UmbStringState("");
  readonly agentId = this.#agentId.asObservable();

  #agentName = new UmbStringState("");
  readonly agentName = this.#agentName.asObservable();

  constructor(host: UmbControllerHost) {
    super(host);
    this.provideContext(UMB_COPILOT_SIDEBAR_CONTEXT, this);
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
    this.#isOpen.setValue(!this.#isOpen.getValue());
  }
}

export const UMB_COPILOT_SIDEBAR_CONTEXT = new UmbContextToken<UmbCopilotSidebarContext>(
  "UmbCopilotSidebarContext",
  undefined,
  (context): context is UmbCopilotSidebarContext =>
    (context as UmbCopilotSidebarContext).IS_COPILOT_SIDEBAR_CONTEXT === true
);
