import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Observable, combineLatest, map } from "rxjs";
import type { UaiStarterPrompt } from "@umbraco-ai/agent";
import type { UaiStarterPromptEntry } from "../context.js";

/**
 * Chip window size: how many entries {@link getStarterPromptsPage} returns per page, and (not
 * coincidentally) the authoring cap of starters per agent -- so a single selected agent's own list
 * always fits one page and never needs the rotate control. Only Auto mode, merging more than one
 * agent's starters, can exceed it.
 */
export const STARTER_PROMPTS_PAGE_SIZE = 4;

/**
 * Whether the rotate control should be shown for a merged list of this many entries. A single
 * selected agent is capped at {@link STARTER_PROMPTS_PAGE_SIZE}, so this is only ever true for an
 * Auto-mode merge spanning more than one agent.
 */
export function hasMultipleStarterPromptPages(entryCount: number): boolean {
    return entryCount > STARTER_PROMPTS_PAGE_SIZE;
}

/**
 * Returns the `page`th window of {@link STARTER_PROMPTS_PAGE_SIZE} entries, wrapping around so
 * repeatedly clicking rotate cycles back to the start. `page` may be any integer, positive or
 * negative.
 */
export function getStarterPromptsPage(entries: UaiStarterPromptEntry[], page: number): UaiStarterPromptEntry[] {
    const pageSize = STARTER_PROMPTS_PAGE_SIZE;
    if (entries.length <= pageSize) return entries;

    const pageCount = Math.ceil(entries.length / pageSize);
    const safePage = ((page % pageCount) + pageCount) % pageCount;
    const start = safePage * pageSize;
    return entries.slice(start, start + pageSize);
}

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
     * Observable of the currently selected agent, including its starter prompts. Also emits the
     * surface's synthetic "Auto" picker option (an id with no matching entry in `availableAgents$`
     * below) or `undefined` before any selection exists -- both cases fall through to the Auto-mode
     * merge.
     */
    selectedAgent$: Observable<UaiStarterPromptAgentSource | undefined>;

    /**
     * Every agent currently available on this surface, already filtered by surface membership and live
     * entity scope (the same rules the server applies) -- exactly what the surface's own agent
     * repository maintains for its picker, minus any synthetic "Auto" entry. Used only to (a) tell a
     * real single-agent selection apart from the Auto placeholder, and (b) supply the agents to merge
     * across in Auto mode. No new availability logic is added here.
     */
    availableAgents$: Observable<UaiStarterPromptAgentSource[]>;

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

        this.starterPrompts$ = combineLatest([config.selectedAgent$, config.availableAgents$]).pipe(
            map(([agent, available]) => UaiStarterPromptsController.toEntries(agent, available)),
        );

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
     * Picks single-agent or Auto-mode aggregation based on whether `agent` matches a real entry in
     * `available` -- the synthetic "Auto" picker option never does (it has no counterpart in the
     * surface's own agent repository), and neither does an as-yet-unresolved `undefined` selection.
     */
    private static toEntries(
        agent: UaiStarterPromptAgentSource | undefined,
        available: UaiStarterPromptAgentSource[],
    ): UaiStarterPromptEntry[] {
        const isSingleAgentSelected = agent !== undefined && available.some((candidate) => candidate.id === agent.id);
        return isSingleAgentSelected
            ? UaiStarterPromptsController.toSingleAgentEntries(agent!)
            : UaiStarterPromptsController.toAggregatedEntries(available);
    }

    /**
     * Maps a single selected agent's starter prompts to display entries, in authoring order. `display`
     * is always the raw prompt in v1 (see {@link UaiStarterPromptEntry}), and `agentName` is left unset
     * -- a single selected agent's own chips never need a tag.
     */
    private static toSingleAgentEntries(agent: UaiStarterPromptAgentSource): UaiStarterPromptEntry[] {
        if (!agent.starterPrompts?.length) return [];
        return agent.starterPrompts.map((starter) => ({
            prompt: starter.prompt,
            display: starter.prompt,
            agentId: agent.id,
        }));
    }

    /**
     * Auto mode: merges every available agent's starter prompts into one list.
     *
     * - Deduped on exact prompt text -- two agents may ship the same seed starter.
     * - Interleaved round-robin, one starter per agent per pass, so an agent with many starters can't
     *   push an agent with few to the back of the list (and out of the first page).
     * - Each surviving entry is tagged with its agent's name, then the tag is stripped from every entry
     *   if everything that survived the dedupe came from a single agent -- a tag on every chip would be
     *   noise when there's nothing to distinguish.
     */
    private static toAggregatedEntries(available: UaiStarterPromptAgentSource[]): UaiStarterPromptEntry[] {
        const queues = available
            .filter((agent) => agent.starterPrompts && agent.starterPrompts.length > 0)
            .map((agent) => ({ agent, remaining: [...agent.starterPrompts!] }));

        const seenPrompts = new Set<string>();
        const merged: { prompt: string; agentId: string; agentName: string }[] = [];

        let tookAny = true;
        while (tookAny) {
            tookAny = false;
            for (const queue of queues) {
                const starter = queue.remaining.shift();
                if (!starter) continue;
                tookAny = true;

                if (seenPrompts.has(starter.prompt)) continue;
                seenPrompts.add(starter.prompt);
                merged.push({ prompt: starter.prompt, agentId: queue.agent.id, agentName: queue.agent.name });
            }
        }

        // Only tag entries once the merge actually spans more than one agent -- a tag on every chip
        // would be noise when there's nothing to distinguish.
        const spansMultipleAgents = new Set(merged.map((entry) => entry.agentId)).size > 1;

        return merged.map((entry) => ({
            prompt: entry.prompt,
            display: entry.prompt,
            agentId: entry.agentId,
            ...(spansMultipleAgents ? { agentName: entry.agentName } : {}),
        }));
    }
}

export default UaiStarterPromptsController;
