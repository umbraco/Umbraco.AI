import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { map } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiAgentRepository } from "@umbraco-ai/agent";
import type { UaiCopilotAgentItem } from "../types.js";

/**
 * Repository for copilot agents.
 * Filters UaiAgentRepository observable to copilot-scoped agents.
 */
export class UaiCopilotAgentRepository {
    #agentRepository: UaiAgentRepository;
    #copilotAgents$: Observable<UaiCopilotAgentItem[]>;

    constructor(host: UmbControllerHost) {
        this.#agentRepository = new UaiAgentRepository(host);

        // Filter agent repository observable to copilot scope at the observable level
        this.#copilotAgents$ = this.#agentRepository.agentItems$.pipe(
            map((items) => {
                return Array.from(items.values())
                    .filter((agent) => agent.scopeIds.includes("copilot"))
                    .map((agent) => ({
                        id: agent.unique,
                        name: agent.name,
                        alias: agent.alias,
                    }));
            }),
        );
    }

    /**
     * Observable of copilot-scoped agents.
     * Derived from agent repository observable with copilot filter.
     */
    get agentItems$(): Observable<UaiCopilotAgentItem[]> {
        return this.#copilotAgents$;
    }

    /**
     * Initialize the underlying agent repository.
     */
    async initialize(): Promise<void> {
        await this.#agentRepository.initialize();
    }
}
