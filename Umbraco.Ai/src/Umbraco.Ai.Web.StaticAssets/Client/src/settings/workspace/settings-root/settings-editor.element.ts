import {
    html,
    css,
    customElement,
    state,
    nothing,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_SETTINGS_WORKSPACE_CONTEXT } from "./settings-workspace.context-token.js";
import type { UaiSettingsModel } from "./settings-workspace.context.js";

// Import profile picker component
import "../../../profile/components/profile-picker/profile-picker.element.js";

@customElement("uai-settings-editor")
export class UaiSettingsEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_SETTINGS_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _loading = true;

    @state()
    private _model: UaiSettingsModel = {
        defaultChatProfileId: null,
        defaultEmbeddingProfileId: null,
    };

    @state()
    private _error: string | null = null;

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

            this.observe(context.error, (error) => {
                this._error = error;
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
            return html`
                <div class="loading-container">
                    <uui-loader-bar></uui-loader-bar>
                </div>
            `;
        }

        return html`
            ${this._error ? html`
                <uui-box>
                    <umb-body-layout>
                        <uui-banner color="danger" look="primary">
                            <uui-icon slot="icon" name="icon-alert"></uui-icon>
                            ${this._error}
                        </uui-banner>
                    </umb-body-layout>
                </uui-box>
            ` : nothing}

            <uui-box headline="Defaults">
                <umb-property-layout
                    label="Default Chat Profile"
                    description="The default profile to use for chat completions when no profile is specified in API calls.">
                    <div slot="editor">
                        <uai-profile-picker
                            capability="Chat"
                            .value=${this._model.defaultChatProfileId ?? undefined}
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
                            .value=${this._model.defaultEmbeddingProfileId ?? undefined}
                            @change=${this.#onEmbeddingProfileChange}>
                        </uai-profile-picker>
                    </div>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            } 

            .loading-container {
                padding: var(--uui-size-layout-2);
            }
            
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }
            
            uui-banner {
                margin-bottom: var(--uui-size-space-5);
            }

            .settings-form {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
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
