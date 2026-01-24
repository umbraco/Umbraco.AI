import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";

/**
 * Workspace view for Connection details.
 * Displays provider settings.
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
                    // Only load provider details when providerId changes, not on every model update.
                    // This prevents unnecessary re-renders that cause cursor jumping in form inputs.
                    const providerChanged = model?.providerId && model.providerId !== this._model?.providerId;
                    this._model = model;
                    if (providerChanged) {
                        this.#loadProviderDetails(model!.providerId);
                    }
                });
            }
        });
    }

    async #loadProviderDetails(providerId: string) {
        const { data } = await this.#providerDetailRepository.requestById(providerId);
        this._provider = data;
    }

    #onSettingsChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ settings: e.detail.model }, "settings")
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`<uui-box headline="General">
            ${this.#renderProviderSettings()}
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
