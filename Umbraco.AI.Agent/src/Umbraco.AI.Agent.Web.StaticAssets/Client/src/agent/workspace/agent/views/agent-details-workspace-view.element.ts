import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbBindToValidation } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiModelEditorChangeEventDetail } from "@umbraco-ai/core";
import type { UaiAgentDetailModel, UaiStandardAgentConfig, UaiOrchestratedAgentConfig, UaiWorkflowItem } from "../../../types.js";
import { isStandardConfig, isOrchestratedConfig } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";
import type { UaiWorkflowPickerElement } from "../../../components/workflow-picker/workflow-picker.element.js";

import "@umbraco-cms/backoffice/markdown-editor";
import "@umbraco-cms/backoffice/code-editor";

/**
 * Workspace view for Agent settings.
 * Renders shared fields (profile, description) for all agent types,
 * plus type-specific fields for standard and orchestrated agents.
 */
@customElement("uai-agent-details-workspace-view")
export class UaiAgentDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_AGENT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiAgentDetailModel;

    @state()
    private _selectedWorkflow?: UaiWorkflowItem;

    // Remembers the schema when toggling to Text, so switching back to Structured restores it
    #cachedOutputSchema?: Record<string, unknown>;

    constructor() {
        super();
        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                    // Clear stale workflow state when not orchestrated
                    if (!model || !isOrchestratedConfig(model.config)) {
                        this._selectedWorkflow = undefined;
                    }
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

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ profileId }, "profileId"),
        );
    }

    // Standard-agent-specific handlers
    #onInstructionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        if (!this._model || !isStandardConfig(this._model.config)) return;
        const config: UaiStandardAgentConfig = {
            ...this._model.config,
            instructions: value || null,
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.instructions"),
        );
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        if (!this._model || !isStandardConfig(this._model.config)) return;
        const config: UaiStandardAgentConfig = {
            ...this._model.config,
            contextIds: picker.value ?? [],
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.contextIds"),
        );
    }

    #onOutputFormatChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const select = event.target as HTMLElement & { value: string };
        if (!this._model || !isStandardConfig(this._model.config)) return;

        let outputSchema: Record<string, unknown> | null;
        if (select.value === "structured") {
            // Restore cached schema, or start with empty object
            outputSchema = this.#cachedOutputSchema ?? {};
        } else {
            // Cache current schema before clearing
            if (this._model.config.outputSchema != null) {
                this.#cachedOutputSchema = this._model.config.outputSchema;
            }
            outputSchema = null;
        }

        const config: UaiStandardAgentConfig = {
            ...this._model.config,
            outputSchema,
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.outputSchema"),
        );
    }

    #onOutputSchemaChange(event: Event) {
        event.stopPropagation();
        const editor = event.target as HTMLElement & { code: string };
        if (!this._model || !isStandardConfig(this._model.config)) return;

        let outputSchema: Record<string, unknown> | null = null;
        try {
            const trimmed = editor.code.trim();
            if (trimmed) {
                outputSchema = JSON.parse(trimmed);
            }
        } catch {
            return; // Don't update on invalid JSON
        }

        const config: UaiStandardAgentConfig = {
            ...this._model.config,
            outputSchema,
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.outputSchema"),
        );
    }

    // Orchestrated-agent-specific handlers
    #onWorkflowLoaded(event: Event) {
        event.stopPropagation();
        const picker = event.target as UaiWorkflowPickerElement;
        this._selectedWorkflow = picker.selectedWorkflow;
    }

    #onWorkflowChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as UaiWorkflowPickerElement;
        const workflowId = picker.value ?? null;
        this._selectedWorkflow = picker.selectedWorkflow;
        if (!this._model || !isOrchestratedConfig(this._model.config)) return;
        const config: UaiOrchestratedAgentConfig = {
            ...this._model.config,
            workflowId,
            settings: workflowId ? {} : null, // Initialize empty settings so defaults are persisted
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.workflowId"),
        );
    }

    #onWorkflowSettingsChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        e.stopPropagation();
        if (!this._model || !isOrchestratedConfig(this._model.config)) return;
        const config: UaiOrchestratedAgentConfig = {
            ...this._model.config,
            settings: e.detail.model,
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.settings"),
        );
    }

    get #standardConfig(): UaiStandardAgentConfig | undefined {
        return this._model && isStandardConfig(this._model.config) ? this._model.config : undefined;
    }

    get #orchestratedConfig(): UaiOrchestratedAgentConfig | undefined {
        return this._model && isOrchestratedConfig(this._model.config) ? this._model.config : undefined;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout label="Description" description="Brief description of this agent">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout
                    label="AI Profile"
                    description=${this._model.agentType === "orchestrated"
                        ? "Select a profile for orchestration-level LLM calls, or leave empty to use the default"
                        : "Select a profile or leave empty to use the default Chat profile from Settings"}
                >
                    <uai-profile-picker
                        slot="editor"
                        .value=${this._model.profileId || undefined}
                        @change=${this.#onProfileChange}
                    ></uai-profile-picker>
                </umb-property-layout>
            </uui-box>

            ${this.#renderStandardSection()}
            ${this.#renderOrchestratedSection()}
        `;
    }

    #renderStandardSection() {
        const config = this.#standardConfig;
        if (!config) return nothing;

        return html`
            <uui-box headline="Agent Behavior">
                <umb-property-layout
                    label="Contexts"
                    description="Predefined contexts to include when running this agent"
                >
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${config.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>

                <umb-property-layout label="Instructions" description="Instructions that define how this agent behaves" mandatory>
                    <umb-input-markdown
                        slot="editor"
                        .value=${config.instructions ?? ""}
                        @change=${this.#onInstructionsChange}
                        required
                        ${umbBindToValidation(this, "$.config.instructions", config.instructions)}
                    ></umb-input-markdown>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Output">
                <umb-property-layout
                    label="Output Format"
                    description="How the agent's response should be formatted"
                >
                    <uui-select
                        slot="editor"
                        .value=${config.outputSchema != null ? "structured" : "text"}
                        .options=${[
                            { name: "Text", value: "text", selected: config.outputSchema == null },
                            { name: "Structured (JSON Schema)", value: "structured", selected: config.outputSchema != null },
                        ]}
                        @change=${this.#onOutputFormatChange}
                        style="width: 100%;"
                    ></uui-select>
                </umb-property-layout>

                ${config.outputSchema != null
                    ? html`
                          <umb-property-layout
                              label="JSON Schema"
                              description="Define the JSON Schema that constrains this agent's output"
                          >
                              <umb-code-editor
                                  slot="editor"
                                  language="json"
                                  .code=${config.outputSchema && Object.keys(config.outputSchema).length > 0
                                      ? JSON.stringify(config.outputSchema, null, 2)
                                      : ""}
                                  disable-minimap
                                  @input=${this.#onOutputSchemaChange}
                              ></umb-code-editor>
                          </umb-property-layout>
                      `
                    : nothing}
            </uui-box>
        `;
    }

    #renderOrchestratedSection() {
        const config = this.#orchestratedConfig;
        if (!config) return nothing;

        return html`
            <uui-box headline="Workflow">
                <umb-property-layout
                    label="Workflow"
                    description="Select the workflow that defines how this orchestrated agent operates"
                >
                    <uai-workflow-picker
                        slot="editor"
                        .value=${config.workflowId ?? undefined}
                        @change=${this.#onWorkflowChange}
                        @workflow-loaded=${this.#onWorkflowLoaded}
                    ></uai-workflow-picker>
                </umb-property-layout>
            </uui-box>

            ${this.#renderWorkflowSettings()}
        `;
    }

    #renderWorkflowSettings() {
        const config = this.#orchestratedConfig;
        if (!config?.workflowId || !this._selectedWorkflow?.settingsSchema) return nothing;

        return html`
            <uai-model-editor
                .schema=${this._selectedWorkflow.settingsSchema}
                .model=${(config.settings as Record<string, unknown>) ?? {}}
                default-group="Workflow Settings"
                empty-message="This workflow has no configurable settings."
                @change=${this.#onWorkflowSettingsChange}
            ></uai-model-editor>
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

            uai-model-editor {
                margin-top: var(--uui-size-layout-1);
            }

            uui-input {
                width: 100%;
            }

            umb-input-markdown {
                width: 100%;
                --umb-code-editor-height: 400px;
            }

            umb-code-editor {
                width: 100%;
                height: 300px;
                --umb-code-editor-height: 300px;
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                overflow: hidden;
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

export default UaiAgentDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-details-workspace-view": UaiAgentDetailsWorkspaceViewElement;
    }
}
