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
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../../api/index.js";

import "@umbraco-cms/backoffice/markdown-editor";

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
    private _workflows: UaiWorkflowItem[] = [];

    @state()
    private _workflowsLoaded = false;

    constructor() {
        super();
        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                    // Load workflows when we detect an orchestrated agent
                    if (model && isOrchestratedConfig(model.config) && !this._workflowsLoaded) {
                        this.#loadWorkflows();
                    }
                });
            }
        });
    }

    async #loadWorkflows() {
        const { data } = await tryExecute(this, AgentsService.getAgentWorkflows());
        this._workflows = (data as UaiWorkflowItem[] | undefined) ?? [];
        this._workflowsLoaded = true;
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

    // Orchestrated-agent-specific handlers
    #onWorkflowChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const select = event.target as HTMLSelectElement;
        const workflowId = select.value || null;
        if (!this._model || !isOrchestratedConfig(this._model.config)) return;
        const config: UaiOrchestratedAgentConfig = {
            ...this._model.config,
            workflowId,
            settings: null, // Reset settings when workflow changes
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

    get #selectedWorkflow(): UaiWorkflowItem | undefined {
        const config = this.#orchestratedConfig;
        if (!config?.workflowId) return undefined;
        return this._workflows.find((w) => w.id === config.workflowId);
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
        `;
    }

    #renderOrchestratedSection() {
        const config = this.#orchestratedConfig;
        if (!config) return nothing;

        const selectedWorkflow = this.#selectedWorkflow;

        return html`
            <uui-box headline="Workflow">
                <umb-property-layout
                    label="Workflow"
                    description="Select the workflow that defines how this orchestrated agent operates"
                >
                    <uui-select
                        slot="editor"
                        .value=${config.workflowId ?? ""}
                        @change=${this.#onWorkflowChange}
                        placeholder="Select a workflow..."
                        .options=${[
                            { name: "Select a workflow...", value: "", selected: !config.workflowId },
                            ...this._workflows.map((w) => ({
                                name: w.name,
                                value: w.id,
                                selected: w.id === config.workflowId,
                            })),
                        ]}
                    ></uui-select>
                </umb-property-layout>

                ${selectedWorkflow?.description
                    ? html`<p class="workflow-description">${selectedWorkflow.description}</p>`
                    : nothing}

                ${selectedWorkflow?.settingsSchema
                    ? html`
                          <uai-model-editor
                              .schema=${selectedWorkflow.settingsSchema}
                              .model=${(config.settings as Record<string, unknown>) ?? {}}
                              empty-message="This workflow has no configurable settings."
                              @change=${this.#onWorkflowSettingsChange}
                          ></uai-model-editor>
                      `
                    : nothing}

                ${!this._workflowsLoaded
                    ? html`<uui-loader-bar></uui-loader-bar>`
                    : this._workflows.length === 0
                      ? html`<p class="no-workflows">No workflows are registered. Workflows are code-based extension points that must be implemented in a .NET project.</p>`
                      : nothing}
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

            uui-input,
            uui-select {
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

            .workflow-description {
                color: var(--uui-color-text-alt);
                font-size: var(--uui-type-small-size);
                margin: 0 var(--uui-size-space-5) var(--uui-size-space-4);
            }

            .no-workflows {
                color: var(--uui-color-text-alt);
                font-style: italic;
                padding: var(--uui-size-space-4) var(--uui-size-space-5);
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
