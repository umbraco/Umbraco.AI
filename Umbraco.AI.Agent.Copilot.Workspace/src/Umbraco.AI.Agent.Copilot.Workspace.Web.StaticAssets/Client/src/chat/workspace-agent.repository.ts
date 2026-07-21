import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { type Observable, map } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiAgentRepository } from "@umbraco-ai/agent";
import type { UaiAgentItem } from "@umbraco-ai/agent-ui";

/** Surface id agents opt into to appear in the Copilot Workspace (matches the backend surface). */
export const COPILOT_WORKSPACE_SURFACE_ID = "copilot-workspace";

/**
 * Agent catalog for the Copilot Workspace: the shared agent list filtered to agents that opt into the
 * `copilot-workspace` surface. Unlike the contextual Copilot there is no section/entity scoping — the
 * Workspace is system-wide (the backend surface declares no scope dimensions), so surface membership
 * is the only filter.
 */
export class UaiWorkspaceAgentRepository extends UmbControllerBase {
    #agentRepository: UaiAgentRepository;
    #agents$: Observable<UaiAgentItem[]>;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#agentRepository = new UaiAgentRepository(host);
        this.#agents$ = this.#agentRepository.agentItems$.pipe(
            map((items) =>
                [...items.values()]
                    .filter((agent) => agent.surfaceIds.includes(COPILOT_WORKSPACE_SURFACE_ID))
                    .map((agent) => ({ id: agent.unique, name: agent.name, alias: agent.alias })),
            ),
        );
    }

    get agentItems$(): Observable<UaiAgentItem[]> {
        return this.#agents$;
    }

    async initialize(): Promise<void> {
        await this.#agentRepository.initialize();
    }
}
