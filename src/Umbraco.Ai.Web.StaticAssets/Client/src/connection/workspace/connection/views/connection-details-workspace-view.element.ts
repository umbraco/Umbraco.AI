import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UAI_EMPTY_GUID, UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";
import "../../../../core/components/model-editor/model-editor.element.js";

/**
 * Workspace view for Connection details.
 * Displays provider (read-only), settings, and active toggle.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
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
                    this._model = model;
                    if (model?.providerId) {
                        this.#loadProviderDetails(model.providerId);
                    }
                });
            }
        });
    }

    async #loadProviderDetails(providerId: string) {
        const { data } = await this.#providerDetailRepository.requestById(providerId);
        this._provider = data;
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: target.checked }, "isActive")
        );
    }

    #onSettingsChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ settings: e.detail.model }, "settings")
        );
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

        return html`<uui-box headline="General">
            ${this.#renderProviderSettings()}
        </uui-box>`;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`<uui-box headline="Info">
            <umb-property-layout label="Id"  orientation="vertical">
               <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
            ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
            : this._model.unique}</div>
            </umb-property-layout>
            
            <umb-property-layout label="Provider"  orientation="vertical">
                <div slot="editor">${this._provider?.name ?? this._model.providerId}</div>
            </umb-property-layout>

            <umb-property-layout label="Capabilities"  orientation="vertical">
                <div slot="editor">
                    ${this._provider?.capabilities.map(cap => html`<uui-tag color="default" look="outline">${cap}</uui-tag> `)}
                </div>
            </umb-property-layout>

            <umb-property-layout label="Active" orientation="vertical">
                <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
            </umb-property-layout>
        </uui-box>`;
    }

    #renderProviderSettings() {
        return html`
            <uai-model-editor
                .schema=${this._provider?.settingsSchema}
                .model=${this._model?.settings}
                empty-message="This provider has no configurable settings."
                @change=${this.#onSettingsChange}>
            </uai-model-editor>
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

export default UaiConnectionDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-details-workspace-view": UaiConnectionDetailsWorkspaceViewElement;
    }
}
