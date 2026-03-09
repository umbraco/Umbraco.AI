import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel, UaiOrchestrationGraph, UaiOrchestratedAgentConfig } from "../../../types.js";
import { isOrchestratedConfig } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

/**
 * Workspace view for the visual workflow graph editor.
 * Only visible for orchestrated agents.
 * Lazy-loads the React Flow graph editor on first render.
 */
@customElement("uai-agent-workflow-workspace-view")
export class UaiAgentWorkflowWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_AGENT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiAgentDetailModel;

    @state()
    private _editorReady = false;

    constructor() {
        super();
        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                    if (model && !this._editorReady) {
                        this.#loadEditor();
                    }
                });
            }
        });
    }

    async #loadEditor() {
        await import("../../../components/orchestration-graph-editor/orchestration-graph-editor.element.js");
        this._editorReady = true;
    }

    #onGraphChanged(event: CustomEvent<UaiOrchestrationGraph>) {
        event.stopPropagation();
        if (!this._model || !isOrchestratedConfig(this._model.config)) return;
        const graph = event.detail;
        const config: UaiOrchestratedAgentConfig = {
            ...this._model.config,
            graph,
        };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ config }, "config.graph"),
        );
    }

    get #graph(): UaiOrchestrationGraph | undefined {
        if (this._model && isOrchestratedConfig(this._model.config)) {
            return this._model.config.graph;
        }
        return undefined;
    }

    render() {
        if (!this._model || !this._editorReady || !this.#graph) return html`<uui-loader></uui-loader>`;

        return html`
            <uai-orchestration-graph-editor
                .graph=${this.#graph}
                @graph-changed=${this.#onGraphChanged}
            ></uai-orchestration-graph-editor>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                height: 100%;
                box-sizing: border-box;
            }

            uai-orchestration-graph-editor {
                height: 100%;
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

export default UaiAgentWorkflowWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-workflow-workspace-view": UaiAgentWorkflowWorkspaceViewElement;
    }
}
