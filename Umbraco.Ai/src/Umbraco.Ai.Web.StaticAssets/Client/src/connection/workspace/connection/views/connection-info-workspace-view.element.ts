import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UAI_EMPTY_GUID, formatDateTime } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";

/**
 * Workspace view for Connection info.
 * Displays provider (read-only), settings, and active toggle.
 */
@customElement("uai-connection-info-workspace-view")
export class UaiConnectionInfoWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;
    #providerDetailRepository = new UaiProviderDetailRepository(this);

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _provider?: UaiProviderDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    // Only load provider info when providerId changes, not on every model update.
                    // This prevents unnecessary re-renders that cause cursor jumping in form inputs.
                    const providerChanged = model?.providerId && model.providerId !== this._model?.providerId;
                    this._model = model;
                    if (providerChanged) {
                        this.#loadProviderInfo(model!.providerId);
                    }
                });
            }
        });
    }

    async #loadProviderInfo(providerId: string) {
        const { data } = await this.#providerDetailRepository.requestById(providerId);
        this._provider = data;
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
            entity-type="connection"
            entity-id=${this._model.unique}
            .currentVersion=${this._model.version}
            @rollback=${() => this.#workspaceContext?.reload()}>
        </uai-version-history>
        `;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`
            <uui-box headline="Info">
                <umb-property-layout label="Id" orientation="vertical">
                    <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
                        ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
                        : this._model.unique}</div>
                </umb-property-layout>
                ${this._model.dateCreated ? html`
                    <umb-property-layout label="Date Created" orientation="vertical">
                        <div slot="editor">${formatDateTime(this._model.dateCreated)}</div>
                    </umb-property-layout>
                ` : ''}
                ${this._model.dateModified ? html`
                    <umb-property-layout label="Date Modified" orientation="vertical">
                        <div slot="editor">${formatDateTime(this._model.dateModified)}</div>
                    </umb-property-layout>
                ` : ''}

                <umb-property-layout label="Provider" orientation="vertical">
                    <div slot="editor">${this._provider?.name ?? this._model.providerId}</div>
                </umb-property-layout>

                <umb-property-layout label="Capabilities" orientation="vertical">
                    <div slot="editor">
                        ${this._provider?.capabilities.map(cap => html`<uui-tag color="default" look="outline">${cap}</uui-tag> `)}
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

export default UaiConnectionInfoWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-info-workspace-view": UaiConnectionInfoWorkspaceViewElement;
    }
}
