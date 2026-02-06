import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { BehaviorSubject, Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent } from "@umbraco-ai/core";
import { UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { AgentsService } from "../../../api/index.js";
import { UaiAgentTypeMapper } from "../../type-mapper.js";
import type { UaiAgentItemModel } from "../../types.js";

/**
 * Options for fetching agents.
 * @public
 */
export interface UaiAgentRepositoryOptions {
    /**
     * Filter agents by scope ID (e.g., "copilot").
     */
    scopeId?: string;

    /**
     * Maximum number of agents to return.
     */
    take?: number;
}

/**
 * Active repository for fetching and observing active agents.
 * Provides observable state management with automatic updates via entity action events.
 * @public
 */
export class UaiAgentRepository extends UmbControllerBase {
    #agentItems$ = new BehaviorSubject<Map<string, UaiAgentItemModel>>(new Map());
    #isInitialized = false;

    constructor(host: UmbControllerHost) {
        super(host);

        // Listen to entity action events
        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            context?.addEventListener(UaiEntityActionEvent.CREATED, this.#onAgentCreatedOrUpdated as EventListener);
            context?.addEventListener(UaiEntityActionEvent.UPDATED, this.#onAgentCreatedOrUpdated as EventListener);
            context?.addEventListener(UaiEntityActionEvent.DELETED, this.#onAgentDeleted as EventListener);
        });
    }

    /**
     * Observable of all active agent items.
     * Emits a new Map whenever agents are added, updated, or removed.
     */
    get agentItems$(): Observable<Map<string, UaiAgentItemModel>> {
        return this.#agentItems$.asObservable();
    }

    /**
     * Initialize the repository by loading all active agents.
     * Should be called once when the repository is first used.
     */
    async initialize(): Promise<void> {
        const { data, error } = await this.fetchActiveAgents();

        if (error || !data) {
            console.warn("[UaiAgentRepository] Failed to load agents:", error);
            return;
        }

        const items = new Map<string, UaiAgentItemModel>();
        data.items.forEach((agent) => {
            items.set(agent.unique, agent);
        });

        this.#agentItems$.next(items);
        this.#isInitialized = true;
    }

    /**
     * Fetches active agents with optional filtering.
     * Only returns agents where isActive is true.
     * @param options - Optional filtering and pagination options
     * @returns Active agents matching the criteria
     */
    async fetchActiveAgents(options?: UaiAgentRepositoryOptions) {
        const { data, error } = await tryExecute(
            this,
            AgentsService.getAllAgents({
                query: {
                    skip: 0,
                    take: options?.take ?? 100,
                    scopeId: options?.scopeId,
                    isActive: true, // Always filter to active agents only
                },
            }),
        );

        if (error || !data) {
            return { error };
        }

        // Map to item model (filtering now happens server-side)
        const items = data.items.map(UaiAgentTypeMapper.toItemModel);

        return {
            data: {
                items,
                total: data.total,
            },
        };
    }

    /**
     * Unified handler for CREATE and UPDATE events.
     * Fetches the agent and adds/updates if active, removes otherwise.
     */
    #onAgentCreatedOrUpdated = (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_AGENT_ENTITY_TYPE) {
            return;
        }

        const unique = event.getUnique();
        if (!unique) {
            return;
        }

        // Async operation - fire and forget
        this.#handleAgentUpdate(unique);
    };

    /**
     * Async helper to fetch and update agent state.
     */
    async #handleAgentUpdate(unique: string): Promise<void> {
        // Fetch all agents to find the updated one
        const { data, error } = await this.fetchActiveAgents({ take: 100 });

        if (error || !data) {
            console.warn("[UaiAgentRepository] Failed to fetch agent:", error);
            return;
        }

        // Find the specific agent
        const agent = data.items.find((a) => a.unique === unique);

        // Remove if not found or not active
        if (!agent || !agent.isActive) {
            this.#removeEntry(unique);
            return;
        }

        // Add or update entry
        this.#addOrUpdateEntry(agent);
    }

    /**
     * Handler for DELETE events.
     * Removes the agent from state.
     */
    #onAgentDeleted = (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_AGENT_ENTITY_TYPE) {
            return;
        }

        const unique = event.getUnique();
        if (!unique) {
            return;
        }
        this.#removeEntry(unique);
    };

    /**
     * Add or update an agent entry in state.
     * Creates a new Map to trigger observable emission.
     */
    #addOrUpdateEntry(agent: UaiAgentItemModel): void {
        const current = new Map(this.#agentItems$.value);
        current.set(agent.unique, agent);
        this.#agentItems$.next(current);
    }

    /**
     * Remove an agent entry from state.
     * Creates a new Map to trigger observable emission.
     */
    #removeEntry(unique: string): void {
        const current = new Map(this.#agentItems$.value);
        if (current.delete(unique)) {
            this.#agentItems$.next(current);
        }
    }
}
