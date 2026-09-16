import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Observable, map } from "rxjs";
import type { UaiStarterPrompt } from "@umbraco-ai/agent";
import type { UaiStarterPromptEntry } from "../context.js";

/**
 * An agent's identity and starter prompts, as seen from whichever agent catalog observable the
 * surface already maintains (Copilot's agent list, Workspace's agent list). Kept minimal and
 * duck-typed so neither surface has to reshape its existing agent item type to satisfy this
 * controller -- both already carry `id`, `name` and (as of this feature) `starterPrompts`.
 */
export interface UaiStarterPromptAgentSource {
    id: string;
    name: string;
    starterPrompts?: UaiStarterPrompt[];
}

/**
 * Configuration for {@link UaiStarterPromptsController}.
 */
export interface UaiStarterPromptsControllerConfig {
    /**
     * Observable of the currently selected agent, including its starter prompts. `undefined` when no
     * single agent is selected (e.g. Auto mode). Auto-mode aggregation across every available agent is
     * Phase 3 -- today an undefined selection simply yields no starters.
     */
    selectedAgent$: Observable<UaiStarterPromptAgentSource | undefined>;

    /** Pins the conversation to the entry's agent -- the same picker move a user makes by hand. */
    selectAgent(agentId: string): void;

    /** Sends the prompt through the same path as typing, so pending-first-message flows stay intact. */
    sendUserMessage(content: string): void | Promise<void>;
}

/**
 * Behaviour layer behind the starter-prompt chips: derives the entries to render for the current
 * selection and dispatches a click through the shared pin-then-send path.
 *
 * Surfaces (Copilot, Copilot Workspace) instantiate this and delegate `starterPrompts$` /
 * `sendStarterPrompt` straight onto their `UaiChatContextApi` implementation.
 */
export class UaiStarterPromptsController extends UmbControllerBase {
    #config: UaiStarterPromptsControllerConfig;
    #currentAgentId?: string;

    readonly starterPrompts$: Observable<UaiStarterPromptEntry[]>;

    constructor(host: UmbControllerHost, config: UaiStarterPromptsControllerConfig) {
        super(host);
        this.#config = config;

        this.starterPrompts$ = config.selectedAgent$.pipe(map((agent) => UaiStarterPromptsController.toEntries(agent)));

        this.observe(config.selectedAgent$, (agent) => {
            this.#currentAgentId = agent?.id;
        });
    }

    /**
     * Pins the entry's agent (if it differs from the one already selected) and sends its prompt --
     * mirroring the "click chip" flow: pin, then send via the exact same path as typing.
     */
    sendStarterPrompt(entry: UaiStarterPromptEntry): void {
        if (entry.agentId && entry.agentId !== this.#currentAgentId) {
            this.#config.selectAgent(entry.agentId);
        }
        void this.#config.sendUserMessage(entry.prompt);
    }

    /**
     * Maps a single selected agent's starter prompts to display entries. `display` is always the raw
     * prompt in v1 (see {@link UaiStarterPromptEntry}), and `agentName` is left unset -- a single
     * selected agent's own chips never need a tag; Auto-mode tagging is Phase 3.
     */
    private static toEntries(agent: UaiStarterPromptAgentSource | undefined): UaiStarterPromptEntry[] {
        if (!agent?.starterPrompts?.length) return [];
        return agent.starterPrompts.map((starter) => ({
            prompt: starter.prompt,
            display: starter.prompt,
            agentId: agent.id,
        }));
    }
}

export default UaiStarterPromptsController;
