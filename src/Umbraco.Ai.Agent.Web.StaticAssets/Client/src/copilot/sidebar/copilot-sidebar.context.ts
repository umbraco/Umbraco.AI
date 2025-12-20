import { UmbContextBase } from "@umbraco-cms/backoffice/class-api";
import { UmbBooleanState, UmbStringState } from "@umbraco-cms/backoffice/observable-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";

export const UMB_COPILOT_SIDEBAR_CONTEXT = new UmbContextToken<UmbCopilotSidebarContext>(
  "UmbCopilotSidebarContext"
);

export interface AgentInfo {
  id: string;
  name: string;
  alias: string;
}

export class UmbCopilotSidebarContext extends UmbContextBase<UmbCopilotSidebarContext> {
  #isOpen = new UmbBooleanState(false);
  readonly isOpen = this.#isOpen.asObservable();

  #agentId = new UmbStringState("");
  readonly agentId = this.#agentId.asObservable();

  #agentName = new UmbStringState("");
  readonly agentName = this.#agentName.asObservable();

  constructor(host: UmbControllerHost) {
    super(host, UMB_COPILOT_SIDEBAR_CONTEXT);
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
