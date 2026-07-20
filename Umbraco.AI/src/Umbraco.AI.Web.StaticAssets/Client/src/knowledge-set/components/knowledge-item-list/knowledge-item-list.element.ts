import { css, customElement, html, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { UaiKnowledgeSetItemDetailModel } from "../../types.js";
import { UAI_KNOWLEDGE_SET_ICON } from "../../constants.js";
import { UAI_KNOWLEDGE_ITEM_MODAL } from "./knowledge-item-modal.token.js";

const elementName = "uai-knowledge-item-list";

/**
 * Read-only mirror of `<uai-resource-list>`.
 *
 * Renders each knowledge item as a `<uui-card-block-type>` (name + description, item icon). Unlike the
 * Context resource list there is no "Add" button, no remove action and no injection-mode tag editing —
 * clicking a card opens the read-only content modal (mirroring where a Context opens its *edit* options
 * modal), which lazily fetches that item's markdown.
 */
@customElement(elementName)
export class UaiKnowledgeItemListElement extends UmbLitElement {
    @property({ type: String, attribute: "knowledge-set-id" })
    public knowledgeSetId = "";

    @property({ type: Array })
    public items: UaiKnowledgeSetItemDetailModel[] = [];

    async #onOpen(item: UaiKnowledgeSetItemDetailModel) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        const modal = modalManager.open(this, UAI_KNOWLEDGE_ITEM_MODAL, {
            data: {
                knowledgeSetId: this.knowledgeSetId,
                item,
            },
        });

        try {
            await modal.onSubmit();
        } catch {
            // Modal closed - read-only viewer, nothing to persist.
        }
    }

    override render() {
        if (!this.items?.length) {
            return html`<p class="empty">${this.localize.term("uaiKnowledgeSet_noItems")}</p>`;
        }

        return html`
            <div class="container">
                ${repeat(
                    this.items,
                    (item) => item.key,
                    (item) => this.#renderItem(item),
                )}
            </div>
        `;
    }

    #renderItem(item: UaiKnowledgeSetItemDetailModel) {
        return html`
            <uui-card-block-type
                name=${item.name}
                .description=${item.description ?? undefined}
                @open=${() => this.#onOpen(item)}
            >
                <umb-icon name=${UAI_KNOWLEDGE_SET_ICON}></umb-icon>
            </uui-card-block-type>
        `;
    }

    static override styles = [
        css`
            :host {
                position: relative;
            }

            .container {
                display: grid;
                gap: var(--uui-size-space-3);
                grid-template-columns: repeat(auto-fill, minmax(var(--umb-card-medium-min-width), 1fr));
                grid-auto-rows: var(--umb-card-medium-min-width);
            }

            .empty {
                color: var(--uui-color-text-alt);
                margin: 0;
            }

            uui-card-block-type {
                cursor: pointer;
                min-width: auto;
            }

            uui-card-block-type:hover {
                background-color: var(--uui-color-surface-emphasis);
            }
        `,
    ];
}

export default UaiKnowledgeItemListElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiKnowledgeItemListElement;
    }
}
