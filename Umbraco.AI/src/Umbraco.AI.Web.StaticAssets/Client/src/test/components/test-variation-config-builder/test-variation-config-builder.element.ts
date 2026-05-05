import { css, html, customElement, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbModalRouteRegistrationController } from "@umbraco-cms/backoffice/router";
import { UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL } from "../../modals/test-variation-config-editor/index.js";
import type { UaiTestVariation } from "../../types.js";
import { getVariationSummary } from "../../types.js";

/**
 * Variation list with add/edit/remove actions.
 *
 * The editor modal is opened via UmbModalRouteRegistrationController + history.pushState
 * (rather than modalManager.open), so the modal carries a router and publishes
 * UMB_ROUTE_CONTEXT to its content. Property editors hosted inside the modal — notably
 * the entity context picker (uai-mock-entity), which itself route-registers a nested
 * editor modal for block grid support — depend on that context being reachable.
 *
 * @fires change - Fires when the variation list changes (UmbChangeEvent).
 */
@customElement("uai-test-variation-config-builder")
export class UaiTestVariationConfigBuilderElement extends UmbLitElement {
    @property({ type: Array })
    variations: UaiTestVariation[] = [];

    @property({ type: String })
    testFeatureId = "";

    /**
     * Route path for the editor modal, emitted by UmbModalRouteRegistrationController
     * once the route is registered (ready shortly after construction).
     */
    #editorRoutePath?: string;

    /**
     * In-flight flow metadata for the current editor invocation. `existingVariation`
     * is undefined for an Add and set to the source variation for an Edit; on submit
     * it tells us whether to insert or replace. Lives only between open and
     * submit/reject. URL cleanup on close is handled by the modal context.
     */
    #pendingFlow?: { existingVariation?: UaiTestVariation };

    constructor() {
        super();

        new UmbModalRouteRegistrationController(this, UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL)
            .addAdditionalPath("variation-editor")
            .onSetup(() => {
                // Guard against stray navigation (e.g. URL replayed via back/forward
                // without going through Add/Edit): refuse to open without pending data.
                if (!this.#pendingFlow) return false;
                return {
                    data: {
                        existingVariation: this.#pendingFlow.existingVariation,
                        testFeatureId: this.testFeatureId,
                    },
                };
            })
            .onSubmit((value) => {
                const flow = this.#pendingFlow;
                if (!flow || !value) {
                    this.#pendingFlow = undefined;
                    return;
                }
                if (flow.existingVariation) {
                    const id = flow.existingVariation.id;
                    this.variations = this.variations.map((v) => (v.id === id ? value.variation : v));
                } else {
                    this.variations = [...this.variations, value.variation];
                }
                this.#pendingFlow = undefined;
                this.dispatchEvent(new UmbChangeEvent());
            })
            .onReject(() => {
                this.#pendingFlow = undefined;
            })
            .observeRouteBuilder((routeBuilder) => {
                this.#editorRoutePath = routeBuilder({});
            });
    }

    #onAdd() {
        this.#pendingFlow = { existingVariation: undefined };
        this.#navigateToEditorRoute();
    }

    #onEdit(variation: UaiTestVariation) {
        this.#pendingFlow = { existingVariation: variation };
        this.#navigateToEditorRoute();
    }

    #onRemove(variationId: string) {
        this.variations = this.variations.filter((v) => v.id !== variationId);
        this.dispatchEvent(new UmbChangeEvent());
    }

    #navigateToEditorRoute() {
        if (!this.#editorRoutePath) {
            // Registration runs synchronously in the constructor, but the route
            // builder observable can fire on the next microtask. If this ever
            // happens in practice, surface it loudly so we can investigate.
            console.error("Variation editor route path not yet registered.");
            this.#pendingFlow = undefined;
            return;
        }
        window.history.pushState({}, "", this.#editorRoutePath);
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
