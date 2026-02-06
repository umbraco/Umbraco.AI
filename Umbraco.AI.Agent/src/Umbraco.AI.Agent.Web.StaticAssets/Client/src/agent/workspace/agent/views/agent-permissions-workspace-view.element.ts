import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { createExtensionApiByAlias } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
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

    @state()
    private _frontendTools: UaiFrontendToolData[] = [];

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
        this.#loadFrontendTools();
    }

    async #loadFrontendTools() {
        try {
            // Use Umbraco's built-in method to instantiate the repository API
            const repository = await createExtensionApiByAlias<UaiFrontendToolRepositoryApi>(
                this,
                "Uai.Repository.FrontendTool",
            );

            this._frontendTools = await repository.getTools();
        } catch {
            // Repository not available (e.g., Copilot not installed)
            this._frontendTools = [];
        }
    }

    #onAllowedToolScopeIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { allowedToolScopeIds: picker.value ?? [] },
                "allowedToolScopeIds",
            ),
        );
    }

    #onAllowedToolIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ allowedToolIds: picker.value ?? [] }, "allowedToolIds"),
        );
    }

    #onUserGroupPermissionsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const component = event.target as any;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { userGroupPermissions: component.value },
                "userGroupPermissions"
            )
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Tool Permissions">
                <umb-property-layout
                    label="Allowed Tool Scopes"
                    description="Select which tool scopes this agent can access. Tools belonging to these scopes will be enabled automatically. System tools are always available."
                >
                    <uai-tool-scope-picker
                        slot="editor"
                        .value=${this._model.allowedToolScopeIds}
                        @change=${this.#onAllowedToolScopeIdsChange}
                    ></uai-tool-scope-picker>
                </umb-property-layout>
                <umb-property-layout
                    label="Allowed Tools"
                    description="Select specific tools to enable for this agent beyond those included in the selected scopes. System tools are always available."
                >
                    <uai-tool-picker
                        slot="editor"
                        .value=${this._model.allowedToolIds}
                        .frontendTools=${this._frontendTools}
                        @change=${this.#onAllowedToolIdsChange}
                    ></uai-tool-picker>
                </umb-property-layout>
                <uai-user-group-tool-permissions
                    .value=${this._model.userGroupPermissions}
                    .agentDefaults=${{
                        allowedToolIds: this._model.allowedToolIds,
                        allowedToolScopeIds: this._model.allowedToolScopeIds
                    }}
                    @change=${this.#onUserGroupPermissionsChange}
                ></uai-user-group-tool-permissions>
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
