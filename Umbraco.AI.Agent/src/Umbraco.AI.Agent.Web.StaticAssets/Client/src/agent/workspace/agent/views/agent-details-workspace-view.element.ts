import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbBindToValidation } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel, UaiAgentScopeRule, UaiAgentScope } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

import "@umbraco-cms/backoffice/markdown-editor";

/**
 * Workspace view for Agent details.
 * Displays system prompt, description, profile, and contexts.
 */
@customElement("uai-agent-details-workspace-view")
export class UaiAgentDetailsWorkspaceViewElement extends UmbLitElement {
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

    #onDescriptionChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ description: value || null }, "description"),
        );
    }

    #onInstructionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ instructions: value || null }, "instructions"),
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ profileId }, "profileId"),
        );
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ contextIds: picker.value ?? [] }, "contextIds"),
        );
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
            <uui-box headline="General">
                <umb-property-layout
                    label="AI Profile"
                    description="Select a profile or leave empty to use the default Chat profile from Settings"
                >
                    <uai-profile-picker
                        slot="editor"
                        .value=${this._model.profileId || undefined}
                        @change=${this.#onProfileChange}
                    ></uai-profile-picker>
                </umb-property-layout>

                <umb-property-layout label="Description" description="Brief description of this agent">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout
                    label="Contexts"
                    description="Predefined contexts to include when running this agent"
                >
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${this._model.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>

                <umb-property-layout label="Instructions" description="Instructions that define how this agent behaves" mandatory>
                    <umb-input-markdown
                        slot="editor"
                        .value=${this._model.instructions ?? ""}
                        @change=${this.#onInstructionsChange}
                        required
                        ${umbBindToValidation(this, "$.instructions", this._model.instructions)}
                    ></umb-input-markdown>
                </umb-property-layout>
            </uui-box>
            <uui-box headline="Surface">
                <umb-property-layout
                    label="Surfaces"
                    description="Select where this agent can be used (e.g., Copilot chat)"
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
                <umb-property-layout
                    label="Allow"
                    description="Prompt is allowed where ANY rule matches (OR logic between rules)"
                >
                    <uai-agent-scope-rules-editor
                        slot="editor"
                        .rules=${this._model.scope?.allowRules ?? []}
                        addButtonLabel="Add Allow Rule"
                        @rules-change=${this.#onAllowRulesChange}
                    ></uai-agent-scope-rules-editor>
                </umb-property-layout>

                <umb-property-layout
                    label="Deny"
                    description="Prompt is denied where ANY rule matches (overrides allow rules)"
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

            uui-input {
                width: 100%;
            }

            umb-input-markdown {
                width: 100%;
                --umb-code-editor-height: 400px;
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

            .scope-section {
                margin-top: var(--uui-size-space-5);
            }

            .section-header {
                margin-bottom: var(--uui-size-space-4);
            }

            .section-header h4 {
                margin: 0 0 var(--uui-size-space-2);
                font-size: 1.1em;
            }

            .help-text {
                margin: 0;
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }

            .examples-box {
                margin-top: var(--uui-size-space-5);
            }

            .examples-box ul {
                margin: var(--uui-size-space-3) 0 0;
                padding-left: var(--uui-size-space-5);
            }

            .examples-box li {
                margin-bottom: var(--uui-size-space-2);
            }
        `,
    ];
}

export default UaiAgentDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-details-workspace-view": UaiAgentDetailsWorkspaceViewElement;
    }
}
