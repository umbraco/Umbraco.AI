import {
    html,
    css,
    customElement,
    state,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_SETTINGS_WORKSPACE_CONTEXT } from "./settings-workspace.context-token.js";
import type { UaiSettingsModel } from "../../types.js";

@customElement("uai-settings-editor")
export class UaiSettingsEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_SETTINGS_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _loading = true;

    @state()
    private _model?: UaiSettingsModel;

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
    }

    #onChatProfileChange(e: UmbChangeEvent): void {
        e.stopPropagation();
        const target = e.target as HTMLElement & { value?: string | string[] };
        const value = target.value;
        this.#workspaceContext?.setDefaultChatProfileId(typeof value === 'string' ? value : null);
    }

    #onEmbeddingProfileChange(e: UmbChangeEvent): void {
        e.stopPropagation();
        const target = e.target as HTMLElement & { value?: string | string[] };
        const value = target.value;
        this.#workspaceContext?.setDefaultEmbeddingProfileId(typeof value === 'string' ? value : null);
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <uui-box headline="Defaults">
                <umb-property-layout
                    label="Default Chat Profile"
                    description="The default profile to use for chat completions when no profile is specified in API calls.">
                    <div slot="editor">
                        <uai-profile-picker
                            capability="Chat"
                            .value=${this._model?.defaultChatProfileId ?? undefined}
                            @change=${this.#onChatProfileChange}>
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
                <umb-property-layout
                    label="Default Embedding Profile"
                    description="The default profile to use for generating embeddings when no profile is specified in API calls.">
                    <div slot="editor">
                        <uai-profile-picker
                            capability="Embedding"
                            .value=${this._model?.defaultEmbeddingProfileId ?? undefined}
                            @change=${this.#onEmbeddingProfileChange}>
                        </uai-profile-picker>
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
