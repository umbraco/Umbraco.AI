import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UaiProfileDetailModel } from "../../../types.js";
import { UAI_EMPTY_GUID, formatDateTime } from "../../../../core/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";

/**
 * Workspace view for Profile info.
 * Displays metadata (read-only) and version history.
 */
@customElement("uai-profile-info-workspace-view")
export class UaiProfileInfoWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;
    #localize = new UmbLocalizationController(this);

    @state()
    private _model?: UaiProfileDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
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

        return html` <uai-version-history
            entity-type="profile"
            entity-id=${this._model.unique}
            .currentVersion=${this._model.version}
            @rollback=${() => this.#workspaceContext?.reload()}
        >
        </uai-version-history>`;
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

                <umb-property-layout label="Capability" orientation="vertical">
                    <div slot="editor">
                        <uui-tag color="default" look="outline"
                            >${this.#getCapabilityLabel(this._model.capability)}</uui-tag
                        >
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

export default UaiProfileInfoWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-info-workspace-view": UaiProfileInfoWorkspaceViewElement;
    }
}
