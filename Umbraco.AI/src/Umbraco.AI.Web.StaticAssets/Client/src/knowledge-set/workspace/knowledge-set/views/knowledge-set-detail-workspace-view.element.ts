import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { codeBlockStyles } from "../../../../core/styles/code-block.styles.js";
import type { UaiKnowledgeSetDetailModel } from "../../../types.js";
import { UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT } from "../knowledge-set-workspace.context-token.js";

/**
 * Read-only workspace view auditing a single knowledge set.
 *
 * Renders the set's metadata and each item's name, description and full markdown content, plus a note
 * that items are surfaced to the AI on demand. Content is shown verbatim (as authored) so an admin can
 * see exactly what the LLM can retrieve. There are no editable fields.
 */
@customElement("uai-knowledge-set-detail-workspace-view")
export class UaiKnowledgeSetDetailWorkspaceViewElement extends UmbLitElement {
    @state()
    private _model?: UaiKnowledgeSetDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.observe(context.model, (model) => {
                this._model = model;
            });
        });
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline=${this._model.name}>
                ${this._model.description ? html`<p id="description">${this._model.description}</p>` : nothing}
                <uui-tag look="secondary" color="default">
                    <uui-icon name="icon-info"></uui-icon>
                    ${this.localize.term("uaiKnowledgeSet_surfacedOnDemand")}
                </uui-tag>
            </uui-box>

            <uui-box headline=${this.localize.term("uaiKnowledgeSet_itemsHeading")}>
                <span slot="header-actions">${this.localize.term("uaiKnowledgeSet_itemCount", this._model.items.length)}</span>
                ${this._model.items.length === 0
                    ? html`<p>${this.localize.term("uaiKnowledgeSet_noItems")}</p>`
                    : html`<div id="items">${this._model.items.map((item) => this.#renderItem(item))}</div>`}
            </uui-box>
        `;
    }

    #renderItem(item: UaiKnowledgeSetDetailModel["items"][number]) {
        return html`
            <div class="item">
                <h4 class="item-name">${item.name}</h4>
                ${item.description ? html`<p class="item-description">${item.description}</p>` : nothing}
                <umb-property-layout label=${this.localize.term("uaiKnowledgeSet_contentLabel")} orientation="vertical">
                    <div slot="editor">
                        <pre class="code-block">${item.content}</pre>
                    </div>
                </umb-property-layout>
            </div>
        `;
    }

    static styles = [
        UmbTextStyles,
        codeBlockStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                --uui-box-default-padding: var(--uui-size-space-5);
            }
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            #description {
                margin-top: 0;
            }

            .item:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
                padding-top: var(--uui-size-layout-1);
                border-top: 1px solid var(--uui-color-divider);
            }

            .item-name {
                margin: 0;
            }

            .item-description {
                color: var(--uui-color-text-alt);
                margin-top: var(--uui-size-space-1);
            }

            uui-loader {
                display: block;
                margin: auto;
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }
        `,
    ];
}

export default UaiKnowledgeSetDetailWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-detail-workspace-view": UaiKnowledgeSetDetailWorkspaceViewElement;
    }
}
