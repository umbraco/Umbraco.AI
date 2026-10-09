import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { type Observable, map } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiAgentRepository, type UaiStarterPrompt } from "@umbraco-ai/agent";
import type { UaiAgentItem } from "@umbraco-ai/agent-ui";

/** Surface id agents opt into to appear in the Copilot Workspace (matches the backend surface). */
export const COPILOT_WORKSPACE_SURFACE_ID = "copilot-workspace";

/**
 * Workspace-specific agent item extending the shared agent item, with each agent's starter prompts for
 * the empty-chat chips. `starterPrompts` is optional so the synthetic "Auto" pseudo-agent (added by the
 * picker when more than one agent is available) can omit it.
 */
export interface UaiWorkspaceAgentItem extends UaiAgentItem {
    starterPrompts?: UaiStarterPrompt[];
}

/**
 * Agent catalog for the Copilot Workspace: the shared agent list filtered to agents that opt into the
 * `copilot-workspace` surface. Unlike the contextual Copilot there is no section/entity scoping — the
 * Workspace is system-wide (the backend surface declares no scope dimensions), so surface membership
 * is the only filter.
 */
export class UaiWorkspaceAgentRepository extends UmbControllerBase {
    #agentRepository: UaiAgentRepository;
    #agents$: Observable<UaiWorkspaceAgentItem[]>;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#agentRepository = new UaiAgentRepository(host);
        this.#agents$ = this.#agentRepository.agentItems$.pipe(
            map((items) =>
                [...items.values()]
                    .filter((agent) => agent.surfaceIds.includes(COPILOT_WORKSPACE_SURFACE_ID))
                    .map((agent) => ({
                        id: agent.unique,
                        name: agent.name,
                        alias: agent.alias,
                        starterPrompts: agent.starterPrompts,
                    })),
            ),
        );
    }

    get agentItems$(): Observable<UaiWorkspaceAgentItem[]> {
        return this.#agents$;
    }

    async initialize(): Promise<void> {
        await this.#agentRepository.initialize();
    }
}
