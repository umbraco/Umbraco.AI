import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiKnowledgeSetDetailModel } from "../../../types.js";
import { UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT } from "../knowledge-set-workspace.context-token.js";
// Ensure the item list element is registered (barrel-exported component).
import "../../../components/index.js";

/**
 * Read-only workspace view for Knowledge Set details.
 *
 * Mirrors `uai-context-details-workspace-view` but read-only: a `<uui-box>` wrapping the
 * `<uai-knowledge-item-list>` card grid (replacing the old inline `<pre>` dump). Each card opens the
 * content modal, which fetches the item's markdown on demand.
 */
@customElement("uai-knowledge-set-details-workspace-view")
export class UaiKnowledgeSetDetailsWorkspaceViewElement extends UmbLitElement {
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
            <uui-box headline="General">
                <umb-property-layout
                    label=${this.localize.term("uaiKnowledgeSet_topicsHeading")}
                    description=${this.localize.term("uaiKnowledgeSet_topicsDescription")}
                >
                    <div slot="editor">
                        <uai-knowledge-item-list
                            knowledge-set-id=${this._model.unique}
                            .items=${this._model.items}
                        ></uai-knowledge-item-list>
                    </div>
                </umb-property-layout>
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

            #description {
                margin-top: 0;
            }

            uui-tag {
                white-space: nowrap;
            }

            umb-property-layout[orientation="vertical"]:not(:last-child) {
                padding-bottom: 0;
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

export default UaiKnowledgeSetDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-details-workspace-view": UaiKnowledgeSetDetailsWorkspaceViewElement;
    }
}
