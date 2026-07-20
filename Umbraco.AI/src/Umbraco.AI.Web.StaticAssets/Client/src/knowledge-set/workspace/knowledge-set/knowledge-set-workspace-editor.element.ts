import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS } from "../constants.js";
import type { UaiKnowledgeSetDetailModel } from "../../types.js";
import { UAI_KNOWLEDGE_SET_ROOT_WORKSPACE_PATH } from "../knowledge-set-root/paths.js";
import { UAI_KNOWLEDGE_SET_WORKSPACE_CONTEXT } from "./knowledge-set-workspace.context-token.js";

/**
 * Read-only workspace editor shell for a single knowledge set.
 *
 * Renders a static header (back button + set name) — unlike the Context editor there is no editable
 * name/alias input, no save, and no entity-action menu. The set's metadata and items are rendered by
 * the read-only details workspace view.
 */
@customElement("uai-knowledge-set-workspace-editor")
export class UaiKnowledgeSetWorkspaceEditorElement extends UmbLitElement {
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
            <umb-workspace-editor alias=${UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS}>
                <div id="header" slot="header">
                    <uui-button
                        href=${UAI_KNOWLEDGE_SET_ROOT_WORKSPACE_PATH}
                        label=${this.localize.term("uaiKnowledgeSet_backToList")}
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <div style="flex: 1;">
                        <span id="name">${this._model.name}</span>
                        <span id="description">${this._model.description}</span>
                    </div>
                    <uui-tag color="primary" look="secondary">${this._model.unique}</uui-tag>
                </div>
            </umb-workspace-editor>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }

            #header {
                display: flex;
                flex: 1 1 auto;
                align-items: center;
                gap: var(--uui-size-space-2);
            }

            #name {
                display: block;
                font-weight: bold;
            }

            #description {
                display: block;
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

export default UaiKnowledgeSetWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-workspace-editor": UaiKnowledgeSetWorkspaceEditorElement;
    }
}
