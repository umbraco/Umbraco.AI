import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Observable, combineLatest, BehaviorSubject } from "@umbraco-cms/backoffice/external/rxjs";
import { map, distinctUntilChanged, switchMap } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiAgentRepository, type UaiAgentScope, type UaiAgentScopeRule } from "@umbraco-ai/agent";
import { UAI_ENTITY_CONTEXT, type UaiEntityContextApi } from "@umbraco-ai/agent-ui";
import type { UaiCopilotAgentItem } from "../types.js";
import { createSectionObservable } from "../section-detector.js";

/**
 * Context dimensions that the copilot surface cares about for agent filtering.
 * The copilot is section-aware and entity-aware, so it checks both dimensions.
 */
const COPILOT_RELEVANT_DIMENSIONS = ["section", "entityType"] as const;

/**
 * Agent availability context for filtering.
 */
interface AgentAvailabilityContext {
    section: string | null;
    entityType: string | null;
}

/**
 * Repository for copilot agents.
 * Filters UaiAgentRepository observable to copilot surface agents with context-aware scope filtering.
 */
export class UaiCopilotAgentRepository extends UmbControllerBase {
    #agentRepository: UaiAgentRepository;
    #copilotAgents$: Observable<UaiCopilotAgentItem[]>;
    #entityContext$ = new BehaviorSubject<UaiEntityContextApi | undefined>(undefined);

    constructor(host: UmbControllerHost) {
        super(host);

        this.#agentRepository = new UaiAgentRepository(host);

        // Consume entity context and push to BehaviorSubject when available
        this.consumeContext(UAI_ENTITY_CONTEXT, (context: UaiEntityContextApi | undefined) => {
            this.#entityContext$.next(context);
        });

        // Create observable for current section
        const section$ = createSectionObservable().pipe(distinctUntilChanged());

        // Create observable for current entity type (from entity context)
        // Switches to the context's entityType$ observable when context becomes available
        const entityType$ = this.#entityContext$.pipe(
            switchMap((context) =>
                context?.entityType$
                    ? context.entityType$.pipe(map((type) => type ?? null))
                    : new Observable<string | null>((subscriber) => subscriber.next(null)),
            ),
            distinctUntilChanged(),
        );

        // Combine agent items with current context to filter reactively
        this.#copilotAgents$ = combineLatest([this.#agentRepository.agentItems$, section$, entityType$]).pipe(
            map(([items, section, entityType]) => {
                const context: AgentAvailabilityContext = {
                    section,
                    entityType,
                };

                const filteredAgents: UaiCopilotAgentItem[] = [];

                for (const agent of items.values()) {

                    // First check: agent must support copilot surface
                    if (!agent.surfaceIds.includes("copilot")) {
                        continue;
                    }

                    // Second check: agent must be available in current context
                    if (!this.#isAgentAvailable(agent.scope, context)) {
                        continue;
                    }

                    filteredAgents.push({
                        id: agent.unique,
                        name: agent.name,
                        alias: agent.alias,
                    });
                }

                return filteredAgents;
            }),
        );
    }

    /**
     * Checks if an agent is available in the given context.
     * Follows the same logic as AIAgentScopeValidator.IsAgentAvailable in the backend.
     *
     * @param scope The agent's scope rules (null means available everywhere)
     * @param context The current context (section, entity type)
     * @returns True if the agent is available, false otherwise
     */
    #isAgentAvailable(scope: UaiAgentScope | null, context: AgentAvailabilityContext): boolean {
        // No scope = available everywhere (backwards compatible)
        if (!scope) {
            return true;
        }

        // Check deny rules first (they take precedence)
        if (this.#isAnyRuleMatched(scope.denyRules, context)) {
            return false;
        }

        // No allow rules = available everywhere (unless denied above)
        if (!scope.allowRules || scope.allowRules.length === 0) {
            return true;
        }

        // Check if any allow rule matches
        return this.#isAnyRuleMatched(scope.allowRules, context);
    }

    /**
     * Checks if any rule in the list matches the current context.
     * OR logic between rules.
     */
    #isAnyRuleMatched(rules: UaiAgentScopeRule[], context: AgentAvailabilityContext): boolean {
        // No rules = no match
        if (!rules || rules.length === 0) {
            return false;
        }

        // Check if any rule matches (OR logic)
        return rules.some((rule) => this.#isRuleMatched(rule, context));
    }

    /**
     * Checks if a single rule matches the current context.
     * AND logic between properties, OR logic within arrays.
     * Only checks dimensions that the copilot surface cares about.
     */
    #isRuleMatched(rule: UaiAgentScopeRule, context: AgentAvailabilityContext): boolean {
        // Check section (if specified AND copilot cares about it)
        if (
            rule.sections &&
            rule.sections.length > 0 &&
            COPILOT_RELEVANT_DIMENSIONS.includes("section")
        ) {
            // No current section = doesn't match
            if (!context.section) {
                return false;
            }

            // Check if current section is in the list (OR logic, case-insensitive)
            if (!rule.sections.some((s: string) => s.toLowerCase() === context.section!.toLowerCase())) {
                return false;
            }
        }

        // Check entity type (if specified AND copilot cares about it)
        if (
            rule.entityTypes &&
            rule.entityTypes.length > 0 &&
            COPILOT_RELEVANT_DIMENSIONS.includes("entityType")
        ) {
            // No current entity type = doesn't match
            if (!context.entityType) {
                return false;
            }

            // Check if current entity type is in the list (OR logic, case-insensitive)
            if (!rule.entityTypes.some((t: string) => t.toLowerCase() === context.entityType!.toLowerCase())) {
                return false;
            }
        }

        // All relevant specified constraints satisfied (AND logic)
        return true;
    }

    /**
     * Observable of copilot surface agents.
     * Derived from agent repository observable with copilot and context-aware filtering.
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
