import { html, css, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_SETTINGS_WORKSPACE_CONTEXT } from "./settings-workspace.context-token.js";
import type { UaiSettingsModel } from "../../types.js";
import type { UaiDisclosureNoticeMode } from "../../../disclosure/types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UaiEnabledCapabilitiesRepository } from "../../../capability/repository/enabled-capabilities.repository.js";

@customElement("uai-settings-editor")
export class UaiSettingsEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_SETTINGS_WORKSPACE_CONTEXT.TYPE;
    #enabledCapabilitiesRepository = new UaiEnabledCapabilitiesRepository(this);

    @state()
    private _loading = true;

    @state()
    private _model?: UaiSettingsModel;

    // undefined while the enabled-capability list hasn't resolved yet, so the
    // experimental pickers stay hidden rather than flashing before disappearing.
    @state()
    private _enabledCapabilities?: string[];

    constructor() {
        super();

        this.consumeContext(UAI_SETTINGS_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.#workspaceContext = context;

            this.observe(context.model, (model) => {
                this._model = model;
            });

            this.observe(context.loading, (loading) => {
                this._loading = loading;
            });
        });

        this.#loadEnabledCapabilities();
    }

    async #loadEnabledCapabilities(): Promise<void> {
        const { data } = await this.#enabledCapabilitiesRepository.getEnabledCapabilities();
        this._enabledCapabilities = data ?? [];
    }

    #onPropertyChange(e: UmbChangeEvent): void {
        e.stopPropagation();
        const target = e.target as HTMLElement & { name?: string; value?: string | string[] };
        const name = target.getAttribute("name") as keyof UaiSettingsModel | undefined;
        if (!name) return;
        const value = typeof target.value === "string" ? target.value : null;
        this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand<UaiSettingsModel>({ [name]: value }, name));
    }

    #isCapabilityEnabled(capability: string): boolean {
        return this._enabledCapabilities?.includes(capability) ?? false;
    }

    #onDisclosureNoticeModeChange(e: Event): void {
        e.stopPropagation();
        const value = (e.target as HTMLElement & { value?: string }).value as UaiDisclosureNoticeMode;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiSettingsModel>({ disclosureNoticeMode: value }, "disclosureNoticeMode"),
        );
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <uui-box headline="Defaults">
                <umb-property-layout
                    label=${this.localize.term("uaiSettings_defaultChatProfileLabel")}
                    description=${this.localize.term("uaiSettings_defaultChatProfileDescription")}
                >
                    <div slot="editor">
                        <uai-profile-picker
                            name="defaultChatProfileId"
                            capability="Chat"
                            .value=${this._model?.defaultChatProfileId ?? undefined}
                            @change=${this.#onPropertyChange}
                        >
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
                <umb-property-layout
                    label=${this.localize.term("uaiSettings_classifierChatProfileLabel")}
                    description=${this.localize.term("uaiSettings_classifierChatProfileDescription")}
                >
                    <div slot="editor">
                        <uai-profile-picker
                            name="classifierChatProfileId"
                            capability="Chat"
                            .value=${this._model?.classifierChatProfileId ?? undefined}
                            @change=${this.#onPropertyChange}
                        >
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
                <umb-property-layout
                    label=${this.localize.term("uaiSettings_defaultEmbeddingProfileLabel")}
                    description=${this.localize.term("uaiSettings_defaultEmbeddingProfileDescription")}
                >
                    <div slot="editor">
                        <uai-profile-picker
                            name="defaultEmbeddingProfileId"
                            capability="Embedding"
                            .value=${this._model?.defaultEmbeddingProfileId ?? undefined}
                            @change=${this.#onPropertyChange}
                        >
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
                <umb-property-layout
                    label=${this.localize.term("uaiSettings_defaultSpeechToTextProfileLabel")}
                    description=${this.localize.term("uaiSettings_defaultSpeechToTextProfileDescription")}
                >
                    <div slot="editor">
                        <uai-profile-picker
                            name="defaultSpeechToTextProfileId"
                            capability="SpeechToText"
                            .value=${this._model?.defaultSpeechToTextProfileId ?? undefined}
                            @change=${this.#onPropertyChange}
                        >
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
                ${this.#isCapabilityEnabled("ImageGeneration")
                    ? html`
                          <umb-property-layout
                              label=${this.localize.term("uaiSettings_defaultImageGenerationProfileLabel")}
                              description=${this.localize.term("uaiSettings_defaultImageGenerationProfileDescription")}
                          >
                              <div slot="editor">
                                  <uai-profile-picker
                                      name="defaultImageGenerationProfileId"
                                      capability="ImageGeneration"
                                      .value=${this._model?.defaultImageGenerationProfileId ?? undefined}
                                      @change=${this.#onPropertyChange}
                                  >
                                  </uai-profile-picker>
                              </div>
                          </umb-property-layout>
                      `
                    : ""}
                ${this.#isCapabilityEnabled("Decision")
                    ? html`
                          <umb-property-layout
                              label=${this.localize.term("uaiSettings_defaultDecisionProfileLabel")}
                              description=${this.localize.term("uaiSettings_defaultDecisionProfileDescription")}
                          >
                              <div slot="editor">
                                  <uai-profile-picker
                                      name="defaultDecisionProfileId"
                                      capability="Decision"
                                      .value=${this._model?.defaultDecisionProfileId ?? undefined}
                                      @change=${this.#onPropertyChange}
                                  >
                                  </uai-profile-picker>
                              </div>
                          </umb-property-layout>
                      `
                    : ""}
            </uui-box>

            <uui-box headline="Transparency">
                <umb-property-layout
                    label="AI Disclosure Notice"
                    description="Shows a notice on chat and prompt responses saying they are AI-generated. You may be legally required to tell people when content is AI-generated, for example under the EU AI Act. Check which rules apply to you before choosing Dismissible or Off."
                >
                    <div slot="editor">
                        <uui-select
                            label="AI Disclosure Notice"
                            .value=${this._model?.disclosureNoticeMode ?? "Always"}
                            .options=${[
                                {
                                    value: "Always",
                                    name: "Always - Always show the notice",
                                    selected: (this._model?.disclosureNoticeMode ?? "Always") === "Always",
                                },
                                {
                                    value: "Dismissible",
                                    name: "Dismissible - Show until each user hides it (remembered in their browser)",
                                    selected: this._model?.disclosureNoticeMode === "Dismissible",
                                },
                                {
                                    value: "Off",
                                    name: "Off - Never show the notice",
                                    selected: this._model?.disclosureNoticeMode === "Off",
                                },
                            ]}
                            @change=${this.#onDisclosureNoticeModeChange}
                        >
                        </uui-select>
                    </div>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-loader {
                display: block;
                margin: auto;
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default UaiSettingsEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-settings-editor": UaiSettingsEditorElement;
    }
}
