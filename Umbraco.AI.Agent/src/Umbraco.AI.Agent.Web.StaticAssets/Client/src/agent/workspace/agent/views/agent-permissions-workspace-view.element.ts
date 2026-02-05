import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

/**
 * Workspace view for Agent tool permissions.
 * Configures which tools and tool scopes an agent can access.
 */
@customElement("uai-agent-permissions-workspace-view")
export class UaiAgentPermissionsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_AGENT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiAgentDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onAllowedToolScopeIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { allowedToolScopeIds: picker.value ?? [] },
                "allowedToolScopeIds"
            )
        );
    }

    #onAllowedToolIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { allowedToolIds: picker.value ?? [] },
                "allowedToolIds"
            )
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Tool Permissions">
                <umb-property-layout
                    label="Allowed Tool Scopes"
                    description="Select which tool scopes this agent can access. Tools belonging to these scopes will be enabled automatically. System tools are always available.">
                    <uai-tool-scope-permissions
                        slot="editor"
                        multiple
                        .value=${this._model.allowedToolScopeIds}
                        @change=${this.#onAllowedToolScopeIdsChange}
                    ></uai-tool-scope-permissions>
                </umb-property-layout>
                <umb-property-layout
                    label="Allowed Tools"
                    description="Select specific tools to enable for this agent beyond those included in the selected scopes. System tools are always available.">
                    <uai-tool-picker
                        slot="editor"
                        .value=${this._model.allowedToolIds}
                        @change=${this.#onAllowedToolIdsChange}
                    ></uai-tool-picker>
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

export default UaiAgentPermissionsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-permissions-workspace-view": UaiAgentPermissionsWorkspaceViewElement;
    }
}
