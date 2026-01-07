import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import type { UUIButtonState } from "@umbraco-cms/backoffice/external/uui";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UAI_EMPTY_GUID, UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";
import { ConnectionsService } from "../../../../api/sdk.gen.js";

/**
 * Workspace view for Connection details.
 * Displays provider (read-only), settings, and active toggle.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;
    #providerDetailRepository = new UaiProviderDetailRepository(this);
    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _provider?: UaiProviderDetailModel;

    @state()
    private _providerSettings: UmbPropertyValueData[] = [];

    @state()
    private _testButtonState?: UUIButtonState;

    @state()
    private _testButtonColor?: "default" | "positive" | "warning" | "danger" = "default"

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                    if (model?.providerId) {
                        this.#loadProviderDetails(model.providerId);
                    }
                    this.#populateProviderSettings();
                });
            }
        });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    async #loadProviderDetails(providerId: string) {
        const { data } = await this.#providerDetailRepository.requestById(providerId);
        this._provider = data;
        this.#populateProviderSettings();
    }

    #populateProviderSettings() {
        if (!this._model || !this._provider) return;
        this._providerSettings = this._provider.settingDefinitions.map((setting) => ({
            alias: setting.key,
            value: this._model!.settings?.[setting.key] ?? setting.defaultValue,
        }));
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: target.checked }, "isActive")
        );
    }

    #onSettingsChange(e: Event) {
        const value = (e.target as UmbPropertyDatasetElement).value;
        const settings = value.reduce((acc, curr) => ({ ...acc, [curr.alias]: curr.value }), {} as Record<string, unknown>);
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ settings }, "settings")
        );
    }

    async #onTestConnection() {
        const unique = this._model?.unique;
        if (!unique || unique === UAI_EMPTY_GUID) return;

        this._testButtonState = "waiting";
        this._testButtonColor = "default";

        const { data, error } = await tryExecute(
            this,
            ConnectionsService.testConnection({ path: { connectionIdOrAlias: unique } })
        );

        if (error || !data?.success) {
            this._testButtonState = "failed";
            this._testButtonColor = "danger";
            this.#notificationContext?.peek("danger", {
                data: { message: data?.errorMessage ?? this.localize.string("#uaiConnection_testConnectionFailed") },
            });
            this.#resetButtonState();
            return;
        }

        this._testButtonState = "success";
        this._testButtonColor = "positive";
        this.#notificationContext?.peek("positive", {
            data: { message: this.localize.string("#uaiConnection_testConnectionSuccess") },
        });
        this.#resetButtonState();
    }

    #resetButtonState() {
        setTimeout(() => {
            this._testButtonState = undefined;
            this._testButtonColor = "default";
        }, 2000);
    }

    #toPropertyConfig(config: unknown): Array<{ alias: string; value: unknown }> {
        if (!config || typeof config !== "object") return [];
        return Object.entries(config).map(([alias, value]) => ({ alias, value }));
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uai-workspace-editor-layout>
                <div>${this.#renderLeftColumn()}</div>
                <div slot="aside">${this.#renderRightColumn()}</div>
            </uai-workspace-editor-layout>
        `;
    }

    #renderLeftColumn() {
        if (!this._model) return null;

        return html`<uui-box headline="General">
            ${this.#renderProviderSettings()}
        </uui-box>`;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`
            <uui-box headline=${this.localize.string("#uaiConnection_actions")}>
                <div class="action-buttons">
                    <uui-button
                        label=${this.localize.string("#uaiConnection_testConnection")}
                        look="primary"
                        .color=${this._testButtonColor}
                        .state=${this._testButtonState}
                        .disabled=${this._model.unique === UAI_EMPTY_GUID}
                        @click=${this.#onTestConnection}>
                        ${this.localize.string("#uaiConnection_testConnection")}
                    </uui-button>
                </div>
            </uui-box>

            <uui-box headline="Info">
                <umb-property-layout label="Id" orientation="vertical">
                    <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
                        ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
                        : this._model.unique}</div>
                </umb-property-layout>

                <umb-property-layout label="Provider" orientation="vertical">
                    <div slot="editor">${this._provider?.name ?? this._model.providerId}</div>
                </umb-property-layout>

                <umb-property-layout label="Capabilities" orientation="vertical">
                    <div slot="editor">
                        ${this._provider?.capabilities.map(cap => html`<uui-tag color="default" look="outline">${cap}</uui-tag> `)}
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Active" orientation="vertical">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>
        `;
    }

    #renderProviderSettings() {
        if (!this._provider) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._provider.settingDefinitions.length === 0) {
            return html`
                <p class="placeholder-text">
                    This provider has no configurable settings.
                </p>
            `;
        }

        return html`
            <umb-property-dataset .value=${this._providerSettings} @change=${this.#onSettingsChange}>
                ${this._provider.settingDefinitions.map(
                    (setting) => html`
                        <umb-property
                            label=${this.localize.string(setting.label)}
                            description=${this.localize.string(setting.description ?? "")}
                            alias=${setting.key}
                            property-editor-ui-alias=${setting.editorUiAlias ?? "Umb.PropertyEditorUi.TextBox"}
                            .config=${setting.editorConfig ? this.#toPropertyConfig(setting.editorConfig) : []}
                            ?mandatory=${setting.isRequired}>
                        </umb-property>
                    `
                )}
            </umb-property-dataset>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            .placeholder-text {
                margin: 0;
                padding: var(--uui-size-space-5) 0;
                color: var(--uui-color-text-alt);
                font-style: italic;
            }

            .provider-display {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
            }

            .provider-display umb-icon {
                font-size: 1.5em;
                color: var(--uui-color-interactive);
            }

            .provider-info {
                display: flex;
                flex-direction: column;
            }

            .provider-info strong {
                color: var(--uui-color-text);
            }

            .provider-info small {
                color: var(--uui-color-text-alt);
            }

            umb-property-layout[orientation="vertical"]:not(:last-child) {
                padding-bottom: 0;
            }

            uui-loader {
                display: block;
                margin: auto;
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }
            
            .action-buttons {
                display: flex;
                gap: var(--uui-size-space-5);
                flex-wrap: wrap;
                padding: var(--uui-size-space-5) 0;
            }
            
            .action-buttons uui-button {
                flex-grow: 1;
            }
        `,
    ];
}

export default UaiConnectionDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-details-workspace-view": UaiConnectionDetailsWorkspaceViewElement;
    }
}
