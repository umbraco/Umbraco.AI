import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "../connection-workspace.context-token.js";

/**
 * Workspace view for Connection details.
 * Displays provider selection, settings, and active toggle.
 */
@customElement("uai-connection-details-workspace-view")
export class UaiConnectionDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _isNew?: boolean;

    constructor() {
        super();
        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => (this._model = model));
                this.observe(context.isNew, (isNew) => (this._isNew = isNew));
            }
        });
    }

    #onProviderChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ providerId: target.value }, "providerId")
        );
    }

    #onActiveChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: target.checked }, "isActive")
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Connection Details">
                <umb-property-layout label="Provider" description="AI provider for this connection">
                    <uui-input
                        slot="editor"
                        .value=${this._model.providerId}
                        @change=${this.#onProviderChange}
                        placeholder="e.g., openai"
                        ?disabled=${!this._isNew}
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Active" description="Enable or disable this connection">
                    <uui-toggle slot="editor" .checked=${this._model.isActive} @change=${this.#onActiveChange}></uui-toggle>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Provider Settings">
                <p class="placeholder-text">
                    Provider-specific settings will be displayed here once the provider is selected and saved. Future
                    enhancement: Dynamic settings form based on provider's SettingDefinitions.
                </p>
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
                margin-bottom: var(--uui-size-layout-1);
            }

            .placeholder-text {
                color: var(--uui-color-text-alt);
                font-style: italic;
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
