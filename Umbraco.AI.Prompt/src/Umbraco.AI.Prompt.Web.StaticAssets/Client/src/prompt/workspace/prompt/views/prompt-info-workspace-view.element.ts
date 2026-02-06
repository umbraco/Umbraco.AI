import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_EMPTY_GUID, formatDateTime } from "@umbraco-ai/core";
import type { UaiPromptDetailModel } from "../../../types.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

/**
 * Workspace view for Prompt info.
 * Displays metadata (read-only) and version history.
 */
@customElement("uai-prompt-info-workspace-view")
export class UaiPromptInfoWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROMPT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiPromptDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROMPT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uai-workspace-editor-layout>
                <div>${this.#renderLeftColumn()}</div>
                <div slot="aside">${this.#renderRightColumn()}</div>
            </uai-workspace-editor-layout>
        `;
    }

    #renderLeftColumn() {
        if (!this._model) return null;

        return html`<uai-version-history
            entity-type="prompt"
            entity-id=${this._model.unique}
            .currentVersion=${this._model.version}
            @rollback=${() => this.#workspaceContext?.reload()}
        >
        </uai-version-history> `;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`
            <uui-box headline="Info">
                <umb-property-layout label="Id" orientation="vertical">
                    <div slot="editor">
                        ${this._model.unique === UAI_EMPTY_GUID
                            ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
                            : this._model.unique}
                    </div>
                </umb-property-layout>
                ${this._model.dateCreated
                    ? html`
                          <umb-property-layout label="Date Created" orientation="vertical">
                              <div slot="editor">${formatDateTime(this._model.dateCreated)}</div>
                          </umb-property-layout>
                      `
                    : ""}
                ${this._model.dateModified
                    ? html`
                          <umb-property-layout label="Date Modified" orientation="vertical">
                              <div slot="editor">${formatDateTime(this._model.dateModified)}</div>
                          </umb-property-layout>
                      `
                    : ""}
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

export default UaiPromptInfoWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-info-workspace-view": UaiPromptInfoWorkspaceViewElement;
    }
}
