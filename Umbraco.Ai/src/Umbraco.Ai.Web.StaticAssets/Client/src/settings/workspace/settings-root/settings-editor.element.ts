import {
    html,
    css,
    customElement,
    state,
    nothing,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { SettingsService } from "../../../api/sdk.gen.js";

// Import profile picker component
import "../../../profile/components/profile-picker/profile-picker.element.js";

interface SettingsModel {
    defaultChatProfileId: string | null;
    defaultEmbeddingProfileId: string | null;
}

@customElement("uai-settings-editor")
export class UaiSettingsEditorElement extends UmbLitElement {
    @state()
    private _loading = true;

    @state()
    private _saving = false;

    @state()
    private _settings: SettingsModel = {
        defaultChatProfileId: null,
        defaultEmbeddingProfileId: null,
    };

    @state()
    private _hasChanges = false;

    @state()
    private _error: string | null = null;

    @state()
    private _success: string | null = null;

    override connectedCallback(): void {
        super.connectedCallback();
        this.#loadSettings();
    }

    async #loadSettings(): Promise<void> {
        this._loading = true;
        this._error = null;

        const { data, error } = await tryExecute(this, SettingsService.getSettings());

        if (error) {
            this._error = "Failed to load settings";
            this._loading = false;
            return;
        }

        if (data) {
            this._settings = {
                defaultChatProfileId: data.defaultChatProfileId ?? null,
                defaultEmbeddingProfileId: data.defaultEmbeddingProfileId ?? null,
            };
        }

        this._loading = false;
    }

    #onChatProfileChange(e: UmbChangeEvent): void {
        e.stopPropagation();
        const target = e.target as HTMLElement & { value?: string | string[] };
        const value = target.value;
        this._settings = {
            ...this._settings,
            defaultChatProfileId: typeof value === 'string' ? value : null,
        };
        this._hasChanges = true;
        this._success = null;
    }

    #onEmbeddingProfileChange(e: UmbChangeEvent): void {
        e.stopPropagation();
        const target = e.target as HTMLElement & { value?: string | string[] };
        const value = target.value;
        this._settings = {
            ...this._settings,
            defaultEmbeddingProfileId: typeof value === 'string' ? value : null,
        };
        this._hasChanges = true;
        this._success = null;
    }

    async #saveSettings(): Promise<void> {
        this._saving = true;
        this._error = null;
        this._success = null;

        const { data, error } = await tryExecute(
            this,
            SettingsService.updateSettings({
                body: {
                    defaultChatProfileId: this._settings.defaultChatProfileId ?? undefined,
                    defaultEmbeddingProfileId: this._settings.defaultEmbeddingProfileId ?? undefined,
                },
            })
        );

        if (error) {
            this._error = "Failed to save settings";
            this._saving = false;
            return;
        }

        if (data) {
            this._settings = {
                defaultChatProfileId: data.defaultChatProfileId ?? null,
                defaultEmbeddingProfileId: data.defaultEmbeddingProfileId ?? null,
            };
        }

        this._hasChanges = false;
        this._success = "Settings saved successfully";
        this._saving = false;
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
            <uui-box headline="Default Profiles">
                <div class="description">
                    Configure the default AI profiles used when no profile is explicitly specified.
                    These settings are stored in the database and take precedence over appsettings.json configuration.
                </div>

                ${this._error ? html`
                    <uui-banner color="danger" look="primary">
                        <uui-icon slot="icon" name="icon-alert"></uui-icon>
                        ${this._error}
                    </uui-banner>
                ` : nothing}

                ${this._success ? html`
                    <uui-banner color="positive" look="primary">
                        <uui-icon slot="icon" name="icon-check"></uui-icon>
                        ${this._success}
                    </uui-banner>
                ` : nothing}

                <div class="settings-form">
                    <umb-property-layout
                        label="Default Chat Profile"
                        description="The default profile to use for chat completions when no profile is specified in API calls.">
                        <div slot="editor">
                            <uai-profile-picker
                                capability="Chat"
                                .value=${this._settings.defaultChatProfileId ?? undefined}
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
                                .value=${this._settings.defaultEmbeddingProfileId ?? undefined}
                                @change=${this.#onEmbeddingProfileChange}>
                            </uai-profile-picker>
                        </div>
                    </umb-property-layout>
                </div>

                <div class="actions">
                    <uui-button
                        label="Save"
                        look="primary"
                        color="positive"
                        ?disabled=${!this._hasChanges || this._saving}
                        @click=${this.#saveSettings}>
                        ${this._saving ? html`<uui-loader-circle></uui-loader-circle>` : 'Save'}
                    </uui-button>
                </div>
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

            .description {
                color: var(--uui-color-text-alt);
                margin-bottom: var(--uui-size-space-5);
            }

            uui-banner {
                margin-bottom: var(--uui-size-space-5);
            }

            .settings-form {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .actions {
                margin-top: var(--uui-size-space-5);
                padding-top: var(--uui-size-space-4);
                border-top: 1px solid var(--uui-color-divider);
                display: flex;
                justify-content: flex-end;
            }

            uui-loader-circle {
                font-size: 12px;
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
