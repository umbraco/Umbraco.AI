// EX-4 — Set a default Extraction profile (AC2-AC5)
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UmbElementControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbObjectState, UmbBooleanState } from "@umbraco-cms/backoffice/observable-api";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

// Stub at the real boundary: the shared enabled-capabilities repository. "Extraction" in the list
// is what the server returns when Umbraco:AI:Experimental:Extraction is on.
const getEnabledCapabilities = vi.fn();
vi.mock("../../../capability/repository/enabled-capabilities.repository.js", () => ({
    UaiEnabledCapabilitiesRepository: class {
        getEnabledCapabilities = getEnabledCapabilities;
    },
}));

import en from "../../../lang/en.js";
import { UAI_SETTINGS_WORKSPACE_CONTEXT } from "./settings-workspace.context-token.js";
import type { UaiSettingsModel } from "../../types.js";
import "./settings-editor.element.js";
import type { UaiSettingsEditorElement } from "./settings-editor.element.js";

// happy-dom doesn't implement ElementInternals. Same stub as settings-editor.element.test.ts.
beforeAll(() => {
    if (!HTMLElement.prototype.attachInternals) {
        HTMLElement.prototype.attachInternals = function () {
            return { setValidity: vi.fn(), form: null } as unknown as ElementInternals;
        };
    }
});

/** Fake workspace context exposing the same observables the editor consumes. */
function provideSettingsContext(host: UmbControllerHost, model: Partial<UaiSettingsModel>) {
    const handleCommand = vi.fn();
    class FakeSettingsWorkspaceContext extends UmbControllerBase {
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

const extractionPicker = (el: HTMLElement) =>
    el.shadowRoot!.querySelector(`uai-profile-picker[name="defaultExtractionProfileId"]`) as
        | (HTMLElement & { value?: string })
        | null;

describe("Feature: default Extraction profile picker", () => {
    afterEach(() => {
        document.body.innerHTML = "";
    });

    describe("Scenario: Extraction is enabled", () => {
        let el: UaiSettingsEditorElement;

        beforeEach(async () => {
            ({ el } = await renderEditor(["Chat", "Embedding", "Extraction"]));
        });

        it.skip("renders a default Extraction profile picker", () => {
            expect(extractionPicker(el)).not.toBeNull();
        });

        it.skip("filters the picker to Extraction profiles", () => {
            expect(extractionPicker(el)?.getAttribute("capability")).toBe("Extraction");
        });
    });

    describe("Scenario: the row is labelled", () => {
        // The editor uses localize.term, which happy-dom renders as the raw key, so the label is
        // asserted at its source (like lang/en.test.ts does for capability labels).
        it.skip('labels it "Default Extraction profile"', () => {
            expect((en.uaiSettings as Record<string, unknown>).defaultExtractionProfileLabel).toBe(
                "Default Extraction profile",
            );
        });
    });

    describe("Scenario: Extraction is enabled and no default is set yet", () => {
        let el: UaiSettingsEditorElement;

        beforeEach(async () => {
            ({ el } = await renderEditor(["Chat", "Extraction"], { defaultExtractionProfileId: null }));
        });

        it.skip("starts the picker empty", () => {
            expect(extractionPicker(el)?.value).toBeUndefined();
        });
    });

    describe("Scenario: Extraction is enabled and a default is stored", () => {
        let el: UaiSettingsEditorElement;

        beforeEach(async () => {
            ({ el } = await renderEditor(["Chat", "Extraction"], {
                defaultExtractionProfileId: "44444444-4444-4444-4444-444444444444",
            }));
        });

        it.skip("shows the stored profile in the picker", () => {
            expect(extractionPicker(el)?.value).toBe("44444444-4444-4444-4444-444444444444");
        });
    });

    describe("Scenario: an Extraction profile is picked", () => {
        let handleCommand: ReturnType<typeof vi.fn>;

        beforeEach(async () => {
            let el: UaiSettingsEditorElement;
            ({ el, handleCommand } = await renderEditor(["Chat", "Extraction"]));
            const picker = extractionPicker(el)!;
            picker.value = "44444444-4444-4444-4444-444444444444";
            picker.dispatchEvent(new UmbChangeEvent());
        });

        it.skip("updates defaultExtractionProfileId on the workspace model", () => {
            const receiver: Partial<UaiSettingsModel> = {};
            handleCommand.mock.calls[0][0].execute(receiver);
            expect(receiver.defaultExtractionProfileId).toBe("44444444-4444-4444-4444-444444444444");
        });
    });

    describe("Sad path", () => {
        describe("Scenario: the Extraction flag is off", () => {
            let el: UaiSettingsEditorElement;

            beforeEach(async () => {
                ({ el } = await renderEditor(["Chat", "Embedding", "SpeechToText", "Decision"], {
                    defaultExtractionProfileId: "44444444-4444-4444-4444-444444444444",
                }));
            });

            it.skip("does not render the Extraction picker", () => {
                expect(extractionPicker(el)).toBeNull();
            });
        });
    });
});
