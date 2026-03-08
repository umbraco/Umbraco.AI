import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiOrchestrationDetailModel, UaiOrchestrationGraph } from "../../../types.js";
import { UAI_ORCHESTRATION_WORKSPACE_CONTEXT } from "../orchestration-workspace.context-token.js";

/**
 * Workspace view for the visual workflow graph editor.
 * Lazy-loads the React Flow graph editor on first render.
 */
@customElement("uai-orchestration-workflow-workspace-view")
export class UaiOrchestrationWorkflowWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_ORCHESTRATION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiOrchestrationDetailModel;

    @state()
    private _editorReady = false;

    constructor() {
        super();
        this.consumeContext(UAI_ORCHESTRATION_WORKSPACE_CONTEXT, (context) => {
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
        const graph = event.detail;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ graph }, "graph"),
        );
    }

    render() {
        if (!this._model || !this._editorReady) return html`<uui-loader></uui-loader>`;

        return html`
            <uai-orchestration-graph-editor
                .graph=${this._model.graph}
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

export default UaiOrchestrationWorkflowWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-workflow-workspace-view": UaiOrchestrationWorkflowWorkspaceViewElement;
    }
}
