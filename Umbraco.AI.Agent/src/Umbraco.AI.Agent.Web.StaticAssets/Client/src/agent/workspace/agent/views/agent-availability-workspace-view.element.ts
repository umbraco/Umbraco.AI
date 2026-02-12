import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel, UaiAgentScopeRule, UaiAgentScope } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

/**
 * Workspace view for Agent availability configuration.
 * Controls where the agent can be used (surfaces) and when it appears (scope rules).
 */
@customElement("uai-agent-availability-workspace-view")
export class UaiAgentAvailabilityWorkspaceViewElement extends UmbLitElement {
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

    #onSurfaceIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ surfaceIds: picker.value ?? [] }, "surfaceIds"),
        );
    }

    #onAllowRulesChange(event: CustomEvent<UaiAgentScopeRule[]>) {
        event.stopPropagation();
        const allowRules = event.detail;
        const currentScope = this._model?.scope ?? { allowRules: [], denyRules: [] };

        const updatedScope: UaiAgentScope = {
            ...currentScope,
            allowRules,
        };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ scope: updatedScope }, "scope"),
        );
    }

    #onDenyRulesChange(event: CustomEvent<UaiAgentScopeRule[]>) {
        event.stopPropagation();
        const denyRules = event.detail;
        const currentScope = this._model?.scope ?? { allowRules: [], denyRules: [] };

        const updatedScope: UaiAgentScope = {
            ...currentScope,
            denyRules,
        };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ scope: updatedScope }, "scope"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Surface">
                <umb-property-layout
                    label="Surfaces"
                    description="Select where this agent can be used (e.g., Copilot chat, API, Dashboard)"
                >
                    <uai-agent-surface-picker
                        slot="editor"
                        multiple
                        .value=${this._model.surfaceIds}
                        @change=${this.#onSurfaceIdsChange}
                    ></uai-agent-surface-picker>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Scope">
                <div class="scope-description">
                    <p>
                        Control when this agent appears based on the current context (section and entity type).
                        Leave empty to show the agent everywhere on selected surfaces.
                    </p>
                </div>

                <umb-property-layout
                    label="Allow Rules"
                    description="Agent appears where ANY rule matches (OR logic between rules)"
                >
                    <uai-agent-scope-rules-editor
                        slot="editor"
                        .rules=${this._model.scope?.allowRules ?? []}
                        addButtonLabel="Add Allow Rule"
                        @rules-change=${this.#onAllowRulesChange}
                    ></uai-agent-scope-rules-editor>
                </umb-property-layout>

                <umb-property-layout
                    label="Deny Rules"
                    description="Agent is hidden where ANY rule matches (overrides allow rules)"
                >
                    <uai-agent-scope-rules-editor
                        slot="editor"
                        .rules=${this._model.scope?.denyRules ?? []}
                        addButtonLabel="Add Deny Rule"
                        @rules-change=${this.#onDenyRulesChange}
                    ></uai-agent-scope-rules-editor>
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

            .scope-description {
                padding: var(--uui-size-space-4) 0;
                color: var(--uui-color-text-alt);
            }

            .scope-description p {
                margin: 0 0 var(--uui-size-space-2);
            }
        `,
    ];
}

export default UaiAgentAvailabilityWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-availability-workspace-view": UaiAgentAvailabilityWorkspaceViewElement;
    }
}
