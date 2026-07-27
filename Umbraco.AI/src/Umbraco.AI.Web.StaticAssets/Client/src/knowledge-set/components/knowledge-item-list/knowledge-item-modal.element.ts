import { css, customElement, html, nothing, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { codeBlockStyles } from "../../../core/styles/code-block.styles.js";
import { UaiKnowledgeSetDetailRepository } from "../../repository/detail/knowledge-set-detail.repository.js";
import type { UaiKnowledgeItemModalData, UaiKnowledgeItemModalValue } from "./knowledge-item-modal.token.js";

const elementName = "uai-knowledge-item-modal";

/**
 * Read-only mirror of `<uai-resource-options-modal>` — where a Context opens that modal to *edit* a
 * resource's settings, a Knowledge Set opens this one to *display* an item's content.
 *
 * Static Name/Description; no model editor, no injection-mode select, no Save — Close only. The item's
 * markdown is fetched lazily from the per-item endpoint when the modal opens, so expensive/computed
 * content is materialised only when actually viewed.
 */
@customElement(elementName)
export class UaiKnowledgeItemModalElement extends UmbModalBaseElement<
    UaiKnowledgeItemModalData,
    UaiKnowledgeItemModalValue
> {
    #repository = new UaiKnowledgeSetDetailRepository(this);

    @state()
    private _loading = true;

    @state()
    private _content = "";

    @state()
    private _error = false;

    override connectedCallback() {
        super.connectedCallback();
        this.#loadContent();
    }

    async #loadContent() {
        if (!this.data) return;

        this._loading = true;
        this._error = false;

        const { data, error } = await this.#repository.requestItemContent(this.data.knowledgeSetId, this.data.item.key);

        if (error || !data) {
            this._error = true;
        } else {
            this._content = data.content;
        }

        this._loading = false;
    }

    #handleClose() {
        this.modalContext?.reject();
    }

    override render() {
        const item = this.data?.item;
        const headline = item?.name ?? this.localize.term("uaiKnowledgeSet_topicHeadline");

        return html`
            <umb-body-layout>
                <div slot="header" class="header-layout">
                    <h3 id="name">${headline}</h3>
                    ${item?.description ? html`<p id="description">${item.description}</p>` : nothing}
                </div>
                <uui-box headline="Content">
                    ${this.#renderContent()}
                </uui-box>

                <div slot="actions">
                    <uui-button label=${this.localize.term("uaiKnowledgeSet_close")} @click=${this.#handleClose}>
                        ${this.localize.term("uaiKnowledgeSet_close")}
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    #renderContent() {
        if (this._loading) return html`<uui-loader></uui-loader>`;
        if (this._error) return html`<p class="error">${this.localize.term("uaiKnowledgeSet_contentError")}</p>`;
        return html`<pre class="code-block">${this._content}</pre>`;
    }

    static override styles = [
        UmbTextStyles,
        codeBlockStyles,
        css`
            uui-box {
                --uui-box-default-padding: var(--uui-size-space-5);
            }

            #name {
                margin: 0;
            }

            #description {
                margin: 0;
                color: var(--uui-color-text-alt);
            }

            uui-tag {
                white-space: nowrap;
            }

            .error {
                color: var(--uui-color-danger);
            }
        `,
    ];
}

export { UaiKnowledgeItemModalElement as element };

export default UaiKnowledgeItemModalElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiKnowledgeItemModalElement;
    }
}
