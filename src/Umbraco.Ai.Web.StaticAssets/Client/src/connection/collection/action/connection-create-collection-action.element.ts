import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiProviderItemRepository } from "../../../provider/repository/item/provider-item.repository.js";
import type { UaiProviderItemModel } from "../../../provider/types.js";
import { UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN } from "../../workspace/connection/paths.js";

/**
 * Collection action element for creating a new connection.
 * Displays a dropdown to select the provider before navigating to the create workspace.
 */
@customElement("uai-connection-create-collection-action")
export class UaiConnectionCreateCollectionActionElement extends UmbLitElement {
    #providerRepository = new UaiProviderItemRepository(this);

    @state()
    private _providers: UaiProviderItemModel[] = [];

    @state()
    private _popoverOpen = false;

    override async connectedCallback() {
        super.connectedCallback();
        await this.#loadProviders();
    }

    async #loadProviders() {
        const { data } = await this.#providerRepository.requestItems();
        this._providers = data ?? [];
    }

    #onSelect(providerId: string) {
        this._popoverOpen = false;
        const path = UAI_CREATE_CONNECTION_WORKSPACE_PATH_PATTERN.generateAbsolute({
            providerAlias: providerId,
        });
        history.pushState(null, "", path);
    }

    #onToggle() {
        this._popoverOpen = !this._popoverOpen;
    }

    override render() {
        return html`
            <uui-button
                look="outline"
                popovertarget="uai-create-popover"
                @click=${this.#onToggle}
            >
                Create
                <uui-symbol-expand .open=${this._popoverOpen}></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="uai-create-popover" placement="bottom-start">
                <umb-popover-layout>
                    ${this._providers.length === 0
                        ? html`<div class="empty">No providers available</div>`
                        : this._providers.map(
                              (provider) => html`
                                  <uui-menu-item
                                      label=${provider.name}
                                      @click=${() => this.#onSelect(provider.id)}
                                  >
                                      <umb-icon slot="icon" name="icon-cloud"></umb-icon>
                                  </uui-menu-item>
                              `
                          )}
                </umb-popover-layout>
            </uui-popover-container>
        `;
    }

    static override styles = [
        css`
            :host {
                display: flex;
                align-items: center;
            }

            .empty {
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-create-collection-action": UaiConnectionCreateCollectionActionElement;
    }
}

export default UaiConnectionCreateCollectionActionElement;
