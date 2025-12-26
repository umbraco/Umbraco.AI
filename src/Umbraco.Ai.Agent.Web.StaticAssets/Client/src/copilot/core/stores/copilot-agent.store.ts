import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject } from "rxjs";
import { UaiCopilotRepository, type CopilotAgentItem } from "../repositories/copilot.repository.js";

/** Loads Copilot agents from the repository and tracks the current selection. */
export class CopilotAgentStore extends UmbControllerBase {
  #repository: UaiCopilotRepository;

  #agents = new BehaviorSubject<CopilotAgentItem[]>([]);
  readonly agents$ = this.#agents.asObservable();

  #selectedAgent = new BehaviorSubject<CopilotAgentItem | undefined>(undefined);
  readonly selectedAgent$ = this.#selectedAgent.asObservable();

  #loading = new BehaviorSubject<boolean>(false);
  readonly loading$ = this.#loading.asObservable();

  constructor(host: UmbControllerHost) {
    super(host);
    this.#repository = new UaiCopilotRepository(host);
  }

  async loadAgents(): Promise<void> {
    if (this.#loading.value) return;

    this.#loading.next(true);
    const { data, error } = await this.#repository.requestActiveAgents();
    this.#loading.next(false);

    if (error || !data) {
      console.error("Failed to load Copilot agents", error);
      return;
    }

    this.#agents.next(data);

    // Auto select first agent when none is selected.
    if (!this.#selectedAgent.value && data.length > 0) {
      this.#selectedAgent.next(data[0]);
    }
  }

  selectAgentById(agentId: string): void {
    const agent = this.#agents.value.find((a) => a.id === agentId);
    if (agent) {
      this.#selectedAgent.next(agent);
    }
  }

  get selectedAgent(): CopilotAgentItem | undefined {
    return this.#selectedAgent.value;
  }
}
