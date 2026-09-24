import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiDecisionRepository } from "../repository/decision.repository.js";
import type {
    UaiBinaryDecisionQuestion,
    UaiBinaryDecisionResult,
    UaiChoiceDecisionQuestion,
    UaiChoiceDecisionResult,
    UaiDecisionOptions,
    UaiDecisionQuestion,
    UaiDecisionResult,
    UaiScoreDecisionQuestion,
    UaiScoreDecisionResult,
} from "../types.js";

/**
 * Public API for asking a Decision-capable AI profile a typed yes/no, pick-one, or score
 * question.
 *
 * Decision is experimental and only available when the `Umbraco:AI:Experimental:Decision`
 * feature flag is enabled server-side; otherwise the server returns 404. A 404 comes back
 * as `error`, never thrown.
 * @public
 */
export class UaiDecisionController extends UmbControllerBase {
    #repository: UaiDecisionRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiDecisionRepository(host);
    }

    /**
     * Asks a yes/no question.
     * @param question - The binary question to ask.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The binary result or error.
     */
    async ask(
        question: UaiBinaryDecisionQuestion,
        options?: UaiDecisionOptions,
    ): Promise<{ data?: UaiBinaryDecisionResult; error?: unknown }>;
    /**
     * Asks a pick-one-of-N question.
     * @param question - The choice question to ask.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The choice result or error.
     */
    async ask(
        question: UaiChoiceDecisionQuestion,
        options?: UaiDecisionOptions,
    ): Promise<{ data?: UaiChoiceDecisionResult; error?: unknown }>;
    /**
     * Asks a question scored against an ordered list of levels.
     * @param question - The score question to ask.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The score result or error.
     */
    async ask(
        question: UaiScoreDecisionQuestion,
        options?: UaiDecisionOptions,
    ): Promise<{ data?: UaiScoreDecisionResult; error?: unknown }>;
    /**
     * Asks a question whose kind isn't known until runtime (e.g. it's forwarded from a
     * caller holding a `UaiDecisionQuestion` union rather than one of its concrete members).
     * @param question - The question to ask, of any {@link UaiDecisionQuestion} kind.
     * @param options - Optional configuration (profile ID/alias, abort signal).
     * @returns The result matching the question's `kind`, or error.
     */
    async ask(
        question: UaiDecisionQuestion,
        options?: UaiDecisionOptions,
    ): Promise<{ data?: UaiDecisionResult; error?: unknown }>;
    async ask(
        question: UaiDecisionQuestion,
        options?: UaiDecisionOptions,
    ): Promise<{ data?: UaiDecisionResult; error?: unknown }> {
        return this.#repository.ask({
            question,
            profileIdOrAlias: options?.profileIdOrAlias,
            signal: options?.signal,
        });
    }
}
