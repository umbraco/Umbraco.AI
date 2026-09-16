import { describe, expect, it, vi } from "vitest";
import { BehaviorSubject } from "rxjs";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import {
    UaiStarterPromptsController,
    getStarterPromptsPage,
    hasMultipleStarterPromptPages,
    STARTER_PROMPTS_PAGE_SIZE,
    type UaiStarterPromptAgentSource,
} from "./starter-prompts.controller.js";

/**
 * Minimal in-memory {@link UmbControllerHost} -- just enough bookkeeping to host the
 * `UmbControllerBase`-derived class under test in a plain vitest environment, with no real Umbraco
 * app/element tree behind it. Mirrors the harness used for `UaiRunController`'s own tests.
 */
function createFakeHost(): UmbControllerHost {
    const controllers = new Set<UmbController>();
    return {
        hasUmbController: (controller) => controllers.has(controller),
        getUmbControllers: (filterMethod) => [...controllers].filter(filterMethod),
        addUmbController: (controller) => void controllers.add(controller),
        removeUmbControllerByAlias: () => {},
        removeUmbController: (controller) => void controllers.delete(controller),
        getHostElement: () => document.createElement("div"),
    };
}

/**
 * @param initialAgent The initially-selected agent (or `undefined` for "no selection yet" / Auto).
 * @param availableAgents$ Every agent the surface considers available -- defaults to just
 *   `initialAgent` (if any), which is enough to exercise the single-agent path. Aggregation tests pass
 *   their own multi-agent list explicitly, with `initialAgent` left `undefined` (Auto mode).
 */
function createHarness(
    initialAgent?: UaiStarterPromptAgentSource,
    availableAgents$: BehaviorSubject<UaiStarterPromptAgentSource[]> = new BehaviorSubject<
        UaiStarterPromptAgentSource[]
    >(initialAgent ? [initialAgent] : []),
) {
    const host = createFakeHost();
    const selectedAgent$ = new BehaviorSubject<UaiStarterPromptAgentSource | undefined>(initialAgent);
    const selectAgent = vi.fn();
    const sendUserMessage = vi.fn();

    const controller = new UaiStarterPromptsController(host, {
        selectedAgent$,
        availableAgents$,
        selectAgent,
        sendUserMessage,
    });

    return { controller, selectedAgent$, availableAgents$, selectAgent, sendUserMessage };
}

function firstValue<T>(source: { subscribe: (fn: (value: T) => void) => unknown }): T {
    let value!: T;
    source.subscribe((v) => (value = v));
    return value;
}

describe("UaiStarterPromptsController — deriving entries", () => {
    it("yields no entries when no agent is selected", () => {
        const { controller } = createHarness(undefined);
        expect(firstValue(controller.starterPrompts$)).toEqual([]);
    });

    it("yields no entries when the selected agent has no starter prompts", () => {
        const { controller } = createHarness({ id: "a1", name: "Agent One", starterPrompts: [] });
        expect(firstValue(controller.starterPrompts$)).toEqual([]);
    });

    it("maps each starter prompt to an entry with display equal to prompt and the agent's id, untagged", () => {
        const { controller } = createHarness({
            id: "a1",
            name: "Agent One",
            starterPrompts: [{ prompt: "Summarize this page" }, { prompt: "Draft a reply" }],
        });

        expect(firstValue(controller.starterPrompts$)).toEqual([
            { prompt: "Summarize this page", display: "Summarize this page", agentId: "a1" },
            { prompt: "Draft a reply", display: "Draft a reply", agentId: "a1" },
        ]);
    });

    it("re-derives entries when the selected agent changes", () => {
        const agentOne: UaiStarterPromptAgentSource = {
            id: "a1",
            name: "Agent One",
            starterPrompts: [{ prompt: "First agent's starter" }],
        };
        const agentTwo: UaiStarterPromptAgentSource = {
            id: "a2",
            name: "Agent Two",
            starterPrompts: [{ prompt: "Second agent's starter" }],
        };
        // Both agents are available throughout -- only the selection moves -- mirroring a real picker
        // where the catalog doesn't change just because the user picks a different entry in it.
        const { controller, selectedAgent$ } = createHarness(agentOne, new BehaviorSubject([agentOne, agentTwo]));

        selectedAgent$.next(agentTwo);

        expect(firstValue(controller.starterPrompts$)).toEqual([
            { prompt: "Second agent's starter", display: "Second agent's starter", agentId: "a2" },
        ]);
    });
});

describe("UaiStarterPromptsController — Auto mode aggregation", () => {
    it("dedupes identical prompt text across two agents", () => {
        const agentOne: UaiStarterPromptAgentSource = {
            id: "a1",
            name: "Agent One",
            starterPrompts: [{ prompt: "Summarize this page" }, { prompt: "Draft a reply" }],
        };
        const agentTwo: UaiStarterPromptAgentSource = {
            id: "a2",
            name: "Agent Two",
            starterPrompts: [{ prompt: "Summarize this page" }, { prompt: "Explain this" }],
        };
        // No single agent selected (undefined) -- Auto mode -- so entries merge across both.
        const { controller } = createHarness(undefined, new BehaviorSubject([agentOne, agentTwo]));

        const prompts = firstValue(controller.starterPrompts$).map((entry) => entry.prompt);

        expect(prompts.filter((prompt) => prompt === "Summarize this page")).toHaveLength(1);
        expect(prompts).toEqual(["Summarize this page", "Draft a reply", "Explain this"]);
    });

    it("round-robins across agents so a 4-starter agent cannot crowd out a 1-starter one", () => {
        const heavy: UaiStarterPromptAgentSource = {
            id: "heavy",
            name: "Heavy Agent",
            starterPrompts: [{ prompt: "H1" }, { prompt: "H2" }, { prompt: "H3" }, { prompt: "H4" }],
        };
        const light: UaiStarterPromptAgentSource = {
            id: "light",
            name: "Light Agent",
            starterPrompts: [{ prompt: "L1" }],
        };
        const { controller } = createHarness(undefined, new BehaviorSubject([heavy, light]));

        const entries = firstValue(controller.starterPrompts$);

        // One-per-agent-per-pass: the light agent's only starter lands second, not crowded to the back
        // by the heavy agent's remaining three -- and so still lands on the first page (window of 4).
        expect(entries.map((entry) => entry.prompt)).toEqual(["H1", "L1", "H2", "H3", "H4"]);
        expect(entries.slice(0, STARTER_PROMPTS_PAGE_SIZE).some((entry) => entry.agentId === "light")).toBe(true);
    });

    it("suppresses the agent tag when the merged list spans exactly one agent", () => {
        const solo: UaiStarterPromptAgentSource = {
            id: "solo",
            name: "Solo Agent",
            starterPrompts: [{ prompt: "Only one" }],
        };
        const empty: UaiStarterPromptAgentSource = { id: "empty", name: "Empty Agent", starterPrompts: [] };
        // Two agents are available, but only one of them actually contributes a starter -- the merge
        // still spans exactly one agent, so the tag must be suppressed even though the picker itself
        // has more than one agent to choose from.
        const { controller } = createHarness(undefined, new BehaviorSubject([solo, empty]));

        expect(firstValue(controller.starterPrompts$)).toEqual([
            { prompt: "Only one", display: "Only one", agentId: "solo" },
        ]);
    });

    it("tags entries with their agent name once the merged list spans more than one agent", () => {
        const agentOne: UaiStarterPromptAgentSource = { id: "a1", name: "Agent One", starterPrompts: [{ prompt: "P1" }] };
        const agentTwo: UaiStarterPromptAgentSource = { id: "a2", name: "Agent Two", starterPrompts: [{ prompt: "P2" }] };
        const { controller } = createHarness(undefined, new BehaviorSubject([agentOne, agentTwo]));

        expect(firstValue(controller.starterPrompts$)).toEqual([
            { prompt: "P1", display: "P1", agentId: "a1", agentName: "Agent One" },
            { prompt: "P2", display: "P2", agentId: "a2", agentName: "Agent Two" },
        ]);
    });

    it("sends an aggregated entry by pinning its agent directly, with no dependency on agent resolution", () => {
        // The controller's config exposes no classifier / agent-resolution observable at all -- pinning
        // is a direct, synchronous call to `selectAgent`, never a wait on a resolved/classified agent.
        const agentOne: UaiStarterPromptAgentSource = {
            id: "a1",
            name: "Agent One",
            starterPrompts: [{ prompt: "Do the thing" }],
        };
        const { controller, selectAgent, sendUserMessage } = createHarness(undefined, new BehaviorSubject([agentOne]));

        const [entry] = firstValue(controller.starterPrompts$);
        controller.sendStarterPrompt(entry);

        expect(selectAgent).toHaveBeenCalledTimes(1);
        expect(selectAgent).toHaveBeenCalledWith("a1");
        expect(sendUserMessage).toHaveBeenCalledTimes(1);
        expect(sendUserMessage).toHaveBeenCalledWith("Do the thing");
    });
});

describe("starter-prompt pagination helpers", () => {
    it("hides the rotate control at 4 or fewer entries", () => {
        expect(hasMultipleStarterPromptPages(0)).toBe(false);
        expect(hasMultipleStarterPromptPages(1)).toBe(false);
        expect(hasMultipleStarterPromptPages(STARTER_PROMPTS_PAGE_SIZE)).toBe(false);
    });

    it("shows the rotate control above 4 entries", () => {
        expect(hasMultipleStarterPromptPages(STARTER_PROMPTS_PAGE_SIZE + 1)).toBe(true);
    });

    it("returns the whole list unpaged when it already fits in one page", () => {
        const entries = [{ prompt: "a", display: "a" }, { prompt: "b", display: "b" }];
        expect(getStarterPromptsPage(entries, 0)).toEqual(entries);
    });

    it("wraps around to the first page again past the last page", () => {
        const entries = Array.from({ length: 5 }, (_, i) => ({ prompt: `p${i}`, display: `p${i}` }));

        expect(getStarterPromptsPage(entries, 0)).toEqual(entries.slice(0, 4));
        expect(getStarterPromptsPage(entries, 1)).toEqual(entries.slice(4, 5));
        expect(getStarterPromptsPage(entries, 2)).toEqual(entries.slice(0, 4));
    });
});

describe("UaiStarterPromptsController — sending a starter prompt", () => {
    it("pins the entry's agent then sends its prompt, when the agent differs from the current selection", () => {
        const { controller, selectAgent, sendUserMessage } = createHarness({
            id: "a1",
            name: "Agent One",
            starterPrompts: [],
        });

        controller.sendStarterPrompt({ prompt: "Do the thing", display: "Do the thing", agentId: "a2" });

        expect(selectAgent).toHaveBeenCalledTimes(1);
        expect(selectAgent).toHaveBeenCalledWith("a2");
        expect(sendUserMessage).toHaveBeenCalledTimes(1);
        expect(sendUserMessage).toHaveBeenCalledWith("Do the thing");
    });

    it("does not re-select the agent already selected", () => {
        const { controller, selectAgent, sendUserMessage } = createHarness({
            id: "a1",
            name: "Agent One",
            starterPrompts: [],
        });

        controller.sendStarterPrompt({ prompt: "Do the thing", display: "Do the thing", agentId: "a1" });

        expect(selectAgent).not.toHaveBeenCalled();
        expect(sendUserMessage).toHaveBeenCalledTimes(1);
        expect(sendUserMessage).toHaveBeenCalledWith("Do the thing");
    });

    it("sends without selecting when the entry carries no agent", () => {
        const { controller, selectAgent, sendUserMessage } = createHarness(undefined);

        controller.sendStarterPrompt({ prompt: "Do the thing", display: "Do the thing" });

        expect(selectAgent).not.toHaveBeenCalled();
        expect(sendUserMessage).toHaveBeenCalledTimes(1);
        expect(sendUserMessage).toHaveBeenCalledWith("Do the thing");
    });
});
