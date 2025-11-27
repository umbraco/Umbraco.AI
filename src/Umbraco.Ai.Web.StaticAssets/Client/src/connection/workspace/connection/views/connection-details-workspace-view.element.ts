import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";

/**
 * Workspace view for Connection details.
 * Displays provider (read-only), settings, and active toggle.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;
    #providerDetailRepository = new UaiProviderDetailRepository(this);

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _provider?: UaiProviderDetailModel;

    @state()
    private _providerSettings: UmbPropertyValueData[] = [];

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

    #toPropertyConfig(config: unknown): Array<{ alias: string; value: unknown }> {
        if (!config || typeof config !== "object") return [];
        return Object.entries(config).map(([alias, value]) => ({ alias, value }));
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <div slot="editor" class="provider-display">
                        <umb-icon name="icon-cloud"></umb-icon>
                        <div class="provider-info">
                            <strong>${this._provider?.name ?? this._model.providerId}</strong>
                            ${this._provider?.capabilities?.length
                                ? html`<small>${this._provider.capabilities.join(", ")}</small>`
                                : null}
                        </div>
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Provider Settings">
                ${this.#renderProviderSettings()}
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
                            label=${setting.label}
                            description=${setting.description ?? ""}
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
        `,
    ];
}

export default UaiConnectionDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-details-workspace-view": UaiConnectionDetailsWorkspaceViewElement;
    }
}
