// DR-3 — Set a default Decision profile (AC2, AC3); DR-6 — Hide disabled experimental capabilities (AC3)
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbObjectState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

// Stub at the real boundary: the shared enabled-capabilities repository (T15).
// ASSUMPTION: module path/name decided in T15; update the mock target to match.
const getEnabledCapabilities = vi.fn();
vi.mock("../../../capability/repository/enabled-capabilities.repository.js", () => ({
    UaiEnabledCapabilitiesRepository: class {
        getEnabledCapabilities = getEnabledCapabilities;
    },
}));

import { UAI_SETTINGS_WORKSPACE_CONTEXT } from "./settings-workspace.context-token.js";
import type { UaiSettingsModel } from "../../types.js";
import "./settings-editor.element.js";
import type { UaiSettingsEditorElement } from "./settings-editor.element.js";

/** Fake workspace context exposing the same observables the editor consumes. */
function provideSettingsContext(host: UmbControllerHost, model: Partial<UaiSettingsModel>) {
    const handleCommand = vi.fn();
    class FakeSettingsWorkspaceContext extends UmbControllerBase {
        model = new UmbObjectState<Partial<UaiSettingsModel>>(model).asObservable();
        loading = new UmbBooleanState(false).asObservable();
        handleCommand = handleCommand;
        constructor(providerHost: UmbControllerHost) {
            super(providerHost);
            this.provideContext(UAI_SETTINGS_WORKSPACE_CONTEXT, this as never);
        }
    }
    new FakeSettingsWorkspaceContext(host);
    return { handleCommand };
}

async function renderEditor(enabled: string[], model: Partial<UaiSettingsModel> = {}) {
    getEnabledCapabilities.mockResolvedValue({ data: enabled });
    const wrapper = document.createElement("div");
    document.body.appendChild(wrapper);
    const host = new UmbElementControllerHost(wrapper);
    host.hostConnected();
    const context = provideSettingsContext(host, model);
    const el = document.createElement("uai-settings-editor") as UaiSettingsEditorElement;
    wrapper.appendChild(el);
    await el.updateComplete;
    await new Promise((r) => setTimeout(r));
    await el.updateComplete;
    return { el, ...context };
}

const picker = (el: HTMLElement, name: string) => el.shadowRoot!.querySelector(`uai-profile-picker[name="${name}"]`);

describe("Feature: settings editor default profile pickers", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: Decision is enabled", () => {
        let el: UaiSettingsEditorElement;

        beforeEach(async () => {
            ({ el } = await renderEditor(["Chat", "Embedding", "SpeechToText", "Decision"]));
        });

        // Pending T15
        it.skip("renders a default Decision profile picker", () => {
            expect(picker(el, "defaultDecisionProfileId")).not.toBeNull();
        });

        // Pending T15
        it.skip("filters the Decision picker to Decision profiles", () => {
            expect(picker(el, "defaultDecisionProfileId")?.getAttribute("capability")).toBe("Decision");
        });
    });

    describe("Scenario: a Decision profile is picked", () => {
        let handleCommand: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiSettingsEditorElement;
            ({ el, handleCommand } = await renderEditor(["Chat", "Decision"]));
            const decisionPicker = picker(el, "defaultDecisionProfileId") as HTMLElement & { value?: string };
            decisionPicker.value = "11111111-1111-1111-1111-111111111111";
            decisionPicker.dispatchEvent(new UmbChangeEvent());
        });

        // Pending T15
        it.skip("updates defaultDecisionProfileId on the workspace model", () => {
            const receiver: Partial<UaiSettingsModel> = {};
            handleCommand.mock.calls[0][0].execute(receiver);
            expect(receiver.defaultDecisionProfileId).toBe("11111111-1111-1111-1111-111111111111");
        });
    });

    describe("Scenario: both experimental flags are off", () => {
        let el: UaiSettingsEditorElement;

        beforeEach(async () => {
            ({ el } = await renderEditor(["Chat", "Embedding", "SpeechToText"]));
        });

        // Pending T15
        it.skip("does not render the Decision picker", () => {
            expect(picker(el, "defaultDecisionProfileId")).toBeNull();
        });

        // Pending T15
        it.skip("does not render the Image Generation picker", () => {
            expect(picker(el, "defaultImageGenerationProfileId")).toBeNull();
        });

        // Pending T15
        it.skip("still renders the non-experimental pickers", () => {
            expect(el.shadowRoot!.querySelectorAll("uai-profile-picker").length).toBe(4);
        });
    });
});
