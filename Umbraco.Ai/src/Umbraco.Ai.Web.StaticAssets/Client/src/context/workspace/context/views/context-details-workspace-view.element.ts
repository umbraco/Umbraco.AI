import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiContextDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONTEXT_WORKSPACE_CONTEXT } from "../context-workspace.context-token.js";
import type { UaiResourceListElement } from "../../../components/resource-list/resource-list.element.js";

/**
 * Workspace view for Context details.
 * Displays and manages context resources.
 */
@customElement("uai-context-details-workspace-view")
export class UaiContextDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONTEXT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiContextDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_CONTEXT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onResourcesChange(e: Event) {
        if (!this._model) return;

        const resourceList = e.target as UaiResourceListElement;
        const resources = resourceList.items;

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "update-resources")
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout label="Resources" description="Define resources to provide additional context to AI operations.">
                    <div slot="editor">
                        <uai-resource-list
                            .items=${this._model.resources}
                            @change=${this.#onResourcesChange}>
                        </uai-resource-list>
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
        `,
    ];
}

export default UaiContextDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-context-details-workspace-view": UaiContextDetailsWorkspaceViewElement;
    }
}
