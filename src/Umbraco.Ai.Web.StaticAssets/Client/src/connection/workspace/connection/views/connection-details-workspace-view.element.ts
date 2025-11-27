import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderItemRepository } from "../../../../provider/repository/item/provider-item.repository.js";
import type { UaiProviderItemModel } from "../../../../provider/types.js";

/**
 * Workspace view for Connection details.
 * Displays provider (read-only), settings, and active toggle.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;
    #providerRepository = new UaiProviderItemRepository(this);

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _providerName?: string;

    @state()
    private _providerCapabilities?: string[];

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
                });
            }
        });
    }

    async #loadProviderDetails(providerId: string) {
        const { data } = await this.#providerRepository.requestItems();
        const provider = data?.find((p: UaiProviderItemModel) => p.id === providerId);
        if (provider) {
            this._providerName = provider.name;
            this._providerCapabilities = provider.capabilities;
        } else {
            this._providerName = undefined;
            this._providerCapabilities = undefined;
        }
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: target.checked }, "isActive")
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <div slot="editor" class="provider-display">
                        <umb-icon name="icon-cloud"></umb-icon>
                        <div class="provider-info">
                            <strong>${this._providerName ?? this._model.providerId}</strong>
                            ${this._providerCapabilities?.length
                                ? html`<small>${this._providerCapabilities.join(", ")}</small>`
                                : null}
                        </div>
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Provider Settings">
                <p class="placeholder-text">
                    Provider-specific settings will be displayed here once the provider is selected and saved. Future
                    enhancement: Dynamic settings form based on provider's SettingDefinitions.
                </p>
            </uui-box>
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
