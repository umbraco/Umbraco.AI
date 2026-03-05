import { css, html, customElement, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL } from "../../modals/test-variation-config-editor/index.js";
import type { UaiTestVariation } from "../../types.js";
import { getVariationSummary } from "../../types.js";

@customElement("uai-test-variation-config-builder")
export class UaiTestVariationConfigBuilderElement extends UmbLitElement {
    @property({ type: Array })
    variations: UaiTestVariation[] = [];

    @property({ type: String })
    testFeatureId = "";

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL, {
            data: {
                testFeatureId: this.testFeatureId,
            },
        });

        try {
            const result = await modal.onSubmit();
            this.variations = [...this.variations, result.variation];
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // User cancelled
        }
    }

    async #onEdit(variation: UaiTestVariation) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL, {
            data: {
                existingVariation: variation,
                testFeatureId: this.testFeatureId,
            },
        });

        try {
            const result = await modal.onSubmit();
            this.variations = this.variations.map((v) => (v.id === variation.id ? result.variation : v));
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // User cancelled
        }
    }

    #onRemove(variationId: string) {
        this.variations = this.variations.filter((v) => v.id !== variationId);
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <uui-ref-list>
                ${repeat(
                    this.variations,
                    (variation) => variation.id,
                    (variation) => html`
                        <uui-ref-node
                            name=${variation.name || "Unnamed variation"}
                            detail=${getVariationSummary(variation)}
                        >
                            <umb-icon slot="icon" name="icon-split-alt color-blue"></umb-icon>
                            <uui-action-bar slot="actions">
                                <uui-button @click=${() => this.#onEdit(variation)} label="Edit">
                                    <uui-icon name="icon-edit"></uui-icon>
                                </uui-button>
                                <uui-button @click=${() => this.#onRemove(variation.id)} label="Remove">
                                    <uui-icon name="icon-trash"></uui-icon>
                                </uui-button>
                            </uui-action-bar>
                        </uui-ref-node>
                    `
                )}
            </uui-ref-list>
            <uui-button class="add-btn" look="placeholder" label="Add Variation" @click=${this.#onAdd}>
                <uui-icon name="icon-add"></uui-icon>
                Add Variation
            </uui-button>
        `;
    }

    static override styles = [
        css`
            .add-btn {
                width: 100%;
            }
        `,
    ];
}

export default UaiTestVariationConfigBuilderElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-variation-config-builder": UaiTestVariationConfigBuilderElement;
    }
}
