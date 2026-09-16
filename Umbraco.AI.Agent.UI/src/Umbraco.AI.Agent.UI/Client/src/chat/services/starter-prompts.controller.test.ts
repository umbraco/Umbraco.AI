import { describe, expect, it, vi } from "vitest";
import { BehaviorSubject } from "rxjs";
import type { UmbController, UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiStarterPromptsController, type UaiStarterPromptAgentSource } from "./starter-prompts.controller.js";

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

function createHarness(initialAgent?: UaiStarterPromptAgentSource) {
    const host = createFakeHost();
    const selectedAgent$ = new BehaviorSubject<UaiStarterPromptAgentSource | undefined>(initialAgent);
    const selectAgent = vi.fn();
    const sendUserMessage = vi.fn();

    const controller = new UaiStarterPromptsController(host, {
        selectedAgent$,
        selectAgent,
        sendUserMessage,
    });

    return { controller, selectedAgent$, selectAgent, sendUserMessage };
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
        const { controller, selectedAgent$ } = createHarness({
            id: "a1",
            name: "Agent One",
            starterPrompts: [{ prompt: "First agent's starter" }],
        });

        selectedAgent$.next({ id: "a2", name: "Agent Two", starterPrompts: [{ prompt: "Second agent's starter" }] });

        expect(firstValue(controller.starterPrompts$)).toEqual([
            { prompt: "Second agent's starter", display: "Second agent's starter", agentId: "a2" },
        ]);
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
