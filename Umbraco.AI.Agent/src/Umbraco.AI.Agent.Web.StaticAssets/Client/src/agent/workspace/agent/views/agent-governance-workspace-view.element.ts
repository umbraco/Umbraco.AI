import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { createExtensionApiByAlias } from "@umbraco-cms/backoffice/extension-registry";
import type { UaiFrontendToolRepositoryApi, UaiFrontendToolData } from "@umbraco-ai/core";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel, UaiStandardAgentConfig } from "../../../types.js";
import { isStandardConfig } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

/**
 * Workspace view for Agent governance settings.
 * Displays guardrails for all agent types, plus tool permissions for standard agents.
 */
@customElement("uai-agent-governance-workspace-view")
export class UaiAgentGovernanceWorkspaceViewElement extends UmbLitElement {
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
            const repository = await createExtensionApiByAlias<UaiFrontendToolRepositoryApi>(
                this,
                "Uai.Repository.FrontendTool",
            );

            this._frontendTools = await repository.getTools();
        } catch {
            this._frontendTools = [];
        }
    }

    get #standardConfig(): UaiStandardAgentConfig | undefined {
        return this._model && isStandardConfig(this._model.config) ? this._model.config : undefined;
    }

    #onGuardrailIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ guardrailIds: picker.value ?? [] }, "guardrailIds"),
        );
    }

    #updateConfig(partial: Partial<UaiStandardAgentConfig>, label: string) {
        const config = this.#standardConfig;
        if (!config) return;
        const updatedConfig: UaiStandardAgentConfig = { ...config, ...partial };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config: updatedConfig }, label),
        );
    }

    #onAllowedToolScopeIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#updateConfig({ allowedToolScopeIds: picker.value ?? [] }, "config.allowedToolScopeIds");
    }

    #onAllowedToolIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#updateConfig({ allowedToolIds: picker.value ?? [] }, "config.allowedToolIds");
    }

    #onUserGroupPermissionsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const component = event.target as any;
        this.#updateConfig({ userGroupPermissions: component.value }, "config.userGroupPermissions");
    }

    #renderToolPermissions() {
        const config = this.#standardConfig;
        if (!config) return nothing;

        return html`
            <uui-box headline="Tool Permissions">
                <umb-property-layout
                    label="Allowed Tool Scopes"
                    description="Select which tool scopes this agent can access. Tools belonging to these scopes will be enabled automatically. System tools are always available."
                >
                    <uai-tool-scope-permissions
                        slot="editor"
                        multiple
                        .value=${config.allowedToolScopeIds}
                        .hideEmptyScopes=${true}
                        @change=${this.#onAllowedToolScopeIdsChange}
                    ></uai-tool-scope-permissions>
                </umb-property-layout>
                <umb-property-layout
                    label="Allowed Tools"
                    description="Select specific tools to enable for this agent beyond those included in the selected scopes. System tools are always available."
                >
                    <uai-tool-picker
                        slot="editor"
                        .value=${config.allowedToolIds}
                        .frontendTools=${this._frontendTools}
                        @change=${this.#onAllowedToolIdsChange}
                    ></uai-tool-picker>
                </umb-property-layout>
                <umb-property-layout
                    label="User Group Permissions"
                    description="Configure tool permissions for specific user groups. Permissions set here will override the general tool and scope permissions for users in those groups."
                >
                    <uai-user-group-tool-permissions
                        slot="editor"
                        .value=${config.userGroupPermissions}
                        .agentDefaults=${{
                            allowedToolIds: config.allowedToolIds,
                            allowedToolScopeIds: config.allowedToolScopeIds
                        }}
                        @change=${this.#onUserGroupPermissionsChange}
                    ></uai-user-group-tool-permissions>
                </umb-property-layout>
            </uui-box>
        `;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            ${this.#renderToolPermissions()}

            <uui-box headline="Guardrails">
                <umb-property-layout label="Guardrails" description="Guardrails to evaluate inputs and responses">
                    <uai-guardrail-picker
                        slot="editor"
                        multiple
                        .value=${this._model.guardrailIds}
                        @change=${this.#onGuardrailIdsChange}
                    ></uai-guardrail-picker>
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

export default UaiAgentGovernanceWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-governance-workspace-view": UaiAgentGovernanceWorkspaceViewElement;
    }
}
