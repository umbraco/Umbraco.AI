import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UaiProviderItemRepository } from "../../../provider/repository/item/provider-item.repository.js";
import type { UaiProviderItemModel } from "../../../provider/types.js";
import type {
    UaiConnectionCreateOptionsModalData,
    UaiConnectionCreateOptionsModalValue,
} from "./connection-create-options-modal.token.js";

@customElement("uai-connection-create-options-modal")
export class UaiConnectionCreateOptionsModalElement extends UmbModalBaseElement<
    UaiConnectionCreateOptionsModalData,
    UaiConnectionCreateOptionsModalValue
> {
    #providerRepository = new UaiProviderItemRepository(this);

    @state()
    private _providers: UaiProviderItemModel[] = [];

    @state()
    private _loading = true;

    override async firstUpdated() {
        await this.#loadProviders();
    }

    async #loadProviders() {
        this._loading = true;
        const { data } = await this.#providerRepository.requestItems();
        this._providers = data ?? [];
        this._loading = false;
    }

    #onSelect(providerId: string) {
        this.value = { providerAlias: providerId };
        this.modalContext?.submit();
    }

    override render() {
        return html`
            <uui-dialog-layout headline=${this.data?.headline ?? "Select Provider"}>
                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._providers.length === 0
                      ? html`<p class="no-providers">No providers available.</p>`
                      : html`
                            <uui-ref-list>
                                ${this._providers.map(
                                    (provider) => html`
                                        <uui-ref-node
                                            name=${provider.name}
                                            detail=${provider.capabilities.join(", ")}
                                            @open=${() => this.#onSelect(provider.id)}
                                            selectable
                                        >
                                            <umb-icon slot="icon" name="icon-cloud"></umb-icon>
                                        </uui-ref-node>
                                    `
                                )}
                            </uui-ref-list>
                        `}
                <uui-button
                    slot="actions"
                    label="Cancel"
                    @click=${() => this.modalContext?.reject()}
                >
                    Cancel
                </uui-button>
            </uui-dialog-layout>
        `;
    }

    static override styles = [
        css`
            uui-loader {
                display: block;
                margin: var(--uui-size-space-4) auto;
            }

            .no-providers {
                color: var(--uui-color-text-alt);
                margin: var(--uui-size-space-4) 0;
            }

            uui-ref-list {
                display: block;
                min-width: 300px;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-create-options-modal": UaiConnectionCreateOptionsModalElement;
    }
}

export default UaiConnectionCreateOptionsModalElement;
