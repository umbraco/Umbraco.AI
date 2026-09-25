// DR-3 — Set a default Decision profile (AC2, AC3); DR-6 — Hide disabled experimental capabilities (AC3)
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbControllerHostElementMixin } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbObjectState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

// Stub at the real boundary: the shared enabled-capabilities repository (T15).
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
        // Required so UAI_SETTINGS_WORKSPACE_CONTEXT's type guard accepts this fake as a match.
        IS_SETTINGS_WORKSPACE_CONTEXT = true;
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

const TEST_HOST_TAG = "uai-settings-editor-test-host";
if (!customElements.get(TEST_HOST_TAG)) {
    customElements.define(TEST_HOST_TAG, class extends UmbControllerHostElementMixin(HTMLElement) {});
}

async function renderEditor(enabled: string[], model: Partial<UaiSettingsModel> = {}) {
    getEnabledCapabilities.mockResolvedValue({ data: enabled });
    const wrapper = document.createElement(TEST_HOST_TAG);
    document.body.appendChild(wrapper);
    const host = wrapper as unknown as UmbControllerHost;
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

        it("renders a default Decision profile picker", () => {
            expect(picker(el, "defaultDecisionProfileId")).not.toBeNull();
        });

        it("filters the Decision picker to Decision profiles", () => {
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

        it("updates defaultDecisionProfileId on the workspace model", () => {
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

        it("does not render the Decision picker", () => {
            expect(picker(el, "defaultDecisionProfileId")).toBeNull();
        });

        it("does not render the Image Generation picker", () => {
            expect(picker(el, "defaultImageGenerationProfileId")).toBeNull();
        });

        it("still renders the non-experimental pickers", () => {
            expect(el.shadowRoot!.querySelectorAll("uai-profile-picker").length).toBe(4);
        });
    });
});
