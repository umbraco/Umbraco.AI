import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UaiTestFeatureItemRepository } from "../../repository/test-feature/test-feature-item.repository.js";
import type { UaiTestFeatureItemModel } from "../../types.js";
import { UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN } from "../../workspace/paths.js";

/**
 * Collection action element for creating a new test.
 * Displays a dropdown to select the test feature before navigating to the create workspace.
 */
@customElement("uai-test-create-collection-action")
export class UaiTestCreateCollectionActionElement extends UmbLitElement {
    #testFeatureRepository = new UaiTestFeatureItemRepository(this);

    @state()
    private _testFeatures: UaiTestFeatureItemModel[] = [];

    @state()
    private _popoverOpen = false;

    override async connectedCallback() {
        super.connectedCallback();
        await this.#loadTestFeatures();
    }

    async #loadTestFeatures() {
        const { data } = await this.#testFeatureRepository.requestItems();
        this._testFeatures = data ?? [];
    }

    #onSelect(testFeatureId: string) {
        this._popoverOpen = false;
        const path = UAI_CREATE_TEST_WORKSPACE_PATH_PATTERN.generateAbsolute({
            testFeatureId,
        });
        history.pushState(null, "", path);
    }

    #onToggle() {
        this._popoverOpen = !this._popoverOpen;
    }

    override render() {
        return html`
            <uui-button look="outline" popovertarget="uai-test-create-popover" @click=${this.#onToggle}>
                Create
                <uui-symbol-expand .open=${this._popoverOpen}></uui-symbol-expand>
            </uui-button>
            <uui-popover-container id="uai-test-create-popover" placement="bottom-start">
                <umb-popover-layout>
                    ${this._testFeatures.length === 0
                        ? html`<div class="empty">No test features available</div>`
                        : this._testFeatures.map(
                              (feature) => html`
                                  <uui-menu-item label=${feature.name} @click=${() => this.#onSelect(feature.id)}>
                                      <umb-icon slot="icon" name="icon-checkbox"></umb-icon>
                                  </uui-menu-item>
                              `,
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
        "uai-test-create-collection-action": UaiTestCreateCollectionActionElement;
    }
}

export default UaiTestCreateCollectionActionElement;
