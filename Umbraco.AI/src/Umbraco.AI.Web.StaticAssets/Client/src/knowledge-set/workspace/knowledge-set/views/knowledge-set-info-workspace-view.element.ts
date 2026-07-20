import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiKnowledgeSetDetailModel } from "../../../types.js";
import { UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT } from "../knowledge-set-workspace.context-token.js";

/**
 * Read-only workspace view for Knowledge Set info.
 *
 * Mirrors `uai-context-info-workspace-view` but Info-only — there is **no** version history, because
 * code-defined knowledge sets are immutable and have no versions. Shows the set's id and metadata.
 */
@customElement("uai-knowledge-set-info-workspace-view")
export class UaiKnowledgeSetInfoWorkspaceViewElement extends UmbLitElement {
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
            <uui-box headline=${this.localize.term("uaiKnowledgeSet_infoHeading")}>
                <umb-property-layout label=${this.localize.term("uaiKnowledgeSet_idLabel")} orientation="vertical">
                    <div slot="editor">${this._model.unique}</div>
                </umb-property-layout>
                <umb-property-layout label=${this.localize.term("uaiLabels_name")} orientation="vertical">
                    <div slot="editor">${this._model.name}</div>
                </umb-property-layout>
                ${this._model.description
                    ? html`
                          <umb-property-layout
                              label=${this.localize.term("uaiKnowledgeSet_descriptionLabel")}
                              orientation="vertical"
                          >
                              <div slot="editor">${this._model.description}</div>
                          </umb-property-layout>
                      `
                    : nothing}
                <umb-property-layout label=${this.localize.term("uaiKnowledgeSet_itemsHeading")} orientation="vertical">
                    <div slot="editor">
                        ${this.localize.term("uaiKnowledgeSet_itemCount", this._model.items.length)}
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

export default UaiKnowledgeSetInfoWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-info-workspace-view": UaiKnowledgeSetInfoWorkspaceViewElement;
    }
}
