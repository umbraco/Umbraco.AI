import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_PROJECT_WORKSPACE_CONTEXT } from "../project-workspace.context-token.js";
import { UAI_EMPTY_GUID, type UaiProjectDetailModel } from "../../types.js";

/** Read-only metadata view for the project workspace (id + timestamps), mirroring the Context Info tab. */
@customElement("uai-copilot-workspace-project-info-view")
export class UaiCopilotWorkspaceProjectInfoViewElement extends UmbLitElement {
    @state() private _model?: UaiProjectDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROJECT_WORKSPACE_CONTEXT, (context) => {
            if (context) this.observe(context.model, (model) => (this._model = model));
        });
    }

    #row(labelKey: string, value: unknown) {
        return html`<div class="row"><span class="key">${this.localize.term(labelKey)}</span><span>${value}</span></div>`;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;
        const isNew = !this._model.unique || this._model.unique === UAI_EMPTY_GUID;
        return html`
            <uui-box headline=${this.localize.term("uaiCopilotWorkspace_projectInfoHeadline")}>
                ${this.#row(
                    "uaiCopilotWorkspace_projectInfoId",
                    isNew ? html`<uui-tag look="secondary">${this.localize.term("uaiCopilotWorkspace_projectInfoUnsaved")}</uui-tag>` : this._model.unique,
                )}
                ${this._model.dateCreated ? this.#row("uaiCopilotWorkspace_projectInfoCreated", this.localize.date(this._model.dateCreated)) : nothing}
                ${this._model.dateModified ? this.#row("uaiCopilotWorkspace_projectInfoModified", this.localize.date(this._model.dateModified)) : nothing}
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
            .row {
                display: flex;
                justify-content: space-between;
                gap: var(--uui-size-space-4);
                padding: var(--uui-size-space-3) 0;
                border-bottom: 1px solid var(--uui-color-divider);
            }
            .row:last-child {
                border-bottom: none;
            }
            .key {
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceProjectInfoViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-project-info-view": UaiCopilotWorkspaceProjectInfoViewElement;
    }
}
