import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiOrchestrationDetailModel } from "../../../types.js";
import { UAI_ORCHESTRATION_WORKSPACE_CONTEXT } from "../orchestration-workspace.context-token.js";

/**
 * Workspace view for Orchestration settings.
 * Configures orchestration behavior: profile, description.
 */
@customElement("uai-orchestration-details-workspace-view")
export class UaiOrchestrationDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_ORCHESTRATION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiOrchestrationDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_ORCHESTRATION_WORKSPACE_CONTEXT, (context) => {
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
            new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>(
                { description: value || null },
                "description",
            ),
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ profileId }, "profileId"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout
                    label="AI Profile"
                    description="Select a profile for orchestration-level LLM calls, or leave empty to use the default"
                >
                    <uai-profile-picker
                        slot="editor"
                        .value=${this._model.profileId || undefined}
                        @change=${this.#onProfileChange}
                    ></uai-profile-picker>
                </umb-property-layout>

                <umb-property-layout label="Description" description="Brief description of this orchestration">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Workflow Graph">
                <div class="graph-placeholder">
                    <uui-icon name="icon-mindmap"></uui-icon>
                    <p>
                        The visual graph editor will be available in a future update.<br />
                        The graph can currently be configured via the Management API.
                    </p>
                    <p class="graph-stats">
                        <strong>${this._model.graph.nodes.length}</strong> nodes,
                        <strong>${this._model.graph.edges.length}</strong> edges
                    </p>
                </div>
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

            .graph-placeholder {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                padding: var(--uui-size-layout-2);
                text-align: center;
                color: var(--uui-color-text-alt);
            }

            .graph-placeholder uui-icon {
                font-size: 48px;
                margin-bottom: var(--uui-size-space-4);
                color: var(--uui-color-border-emphasis);
            }

            .graph-stats {
                margin-top: var(--uui-size-space-3);
                color: var(--uui-color-text);
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

export default UaiOrchestrationDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-details-workspace-view": UaiOrchestrationDetailsWorkspaceViewElement;
    }
}
