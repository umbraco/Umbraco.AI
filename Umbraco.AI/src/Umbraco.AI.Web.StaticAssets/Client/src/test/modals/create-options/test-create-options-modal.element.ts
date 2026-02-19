import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UaiTestFeatureItemRepository } from "../../repository/test-feature/test-feature-item.repository.js";
import type { UaiTestFeatureItemModel } from "../../types.js";
import type {
    UaiTestCreateOptionsModalData,
    UaiTestCreateOptionsModalValue,
} from "./test-create-options-modal.token.js";

@customElement("uai-test-create-options-modal")
export class UaiTestCreateOptionsModalElement extends UmbModalBaseElement<
    UaiTestCreateOptionsModalData,
    UaiTestCreateOptionsModalValue
> {
    #repository = new UaiTestFeatureItemRepository(this);

    @state()
    private _testFeatures: UaiTestFeatureItemModel[] = [];

    @state()
    private _loading = true;

    override async firstUpdated() {
        await this.#loadTestFeatures();
    }

    async #loadTestFeatures() {
        this._loading = true;
        const { data } = await this.#repository.requestItems();
        this._testFeatures = data ?? [];
        this._loading = false;
    }

    #onSelect(testFeatureId: string) {
        this.value = { testFeatureId };
        this.modalContext?.submit();
    }

    override render() {
        return html`
            <uui-dialog-layout headline=${this.data?.headline ?? "Select Test Feature"}>
                ${this._loading
                    ? html`<uui-loader></uui-loader>`
                    : this._testFeatures.length === 0
                      ? html`<p class="no-features">No test features available.</p>`
                      : html`
                            <uui-ref-list>
                                ${this._testFeatures.map(
                                    (feature) => html`
                                        <uui-ref-node
                                            name=${feature.name}
                                            detail=${feature.description ?? feature.category ?? ""}
                                            select-only
                                            selectable
                                            @selected=${() => this.#onSelect(feature.id)}
                                            @open=${() => this.#onSelect(feature.id)}
                                        >
                                            <umb-icon slot="icon" name="icon-lab"></umb-icon>
                                        </uui-ref-node>
                                    `,
                                )}
                            </uui-ref-list>
                        `}
                <uui-button slot="actions" label="Cancel" @click=${() => this.modalContext?.reject()}>
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

            .no-features {
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
        "uai-test-create-options-modal": UaiTestCreateOptionsModalElement;
    }
}

export default UaiTestCreateOptionsModalElement;
