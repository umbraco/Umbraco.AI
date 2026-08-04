import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UUIInputElement, UUITextareaElement } from "@umbraco-cms/backoffice/external/uui";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiContextPickerElement } from "@umbraco-ai/core";
import { UAI_PROJECT_WORKSPACE_CONTEXT } from "../project-workspace.context-token.js";
import type { ContextResourceModel, UaiProjectDetailModel } from "../../types.js";

/** Minimal structural type for the globally-registered (but not type-exported) `uai-resource-list`. */
interface ResourceListElement extends HTMLElement {
    items: ContextResourceModel[];
}

/**
 * Details view for the project workspace — the shared instructions, attached contexts, and resources
 * every conversation in the project inherits. Standard `uui-box` + `umb-property-layout` rows; writes
 * go through the workspace context's command handler (like the core Context view).
 */
@customElement("uai-copilot-workspace-project-details-view")
export class UaiCopilotWorkspaceProjectDetailsViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROJECT_WORKSPACE_CONTEXT.TYPE;

    @state() private _model?: UaiProjectDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROJECT_WORKSPACE_CONTEXT, (context) => {
            this.#workspaceContext = context;
            if (context) this.observe(context.model, (model) => (this._model = model));
        });
    }

    #update(part: Partial<UaiProjectDetailModel>, correlationId: string) {
        this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand<UaiProjectDetailModel>(part, correlationId));
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;
        return html`
            <uui-box headline=${this.localize.term("uaiCopilotWorkspace_projectDetailsHeadline")}>
                <umb-property-layout label=${this.localize.term("uaiCopilotWorkspace_projectDescriptionLabel")}>
                    <div slot="editor">
                        <uui-input
                            .value=${this._model.description ?? ""}
                            @input=${(e: InputEvent) => this.#update({ description: (e.target as UUIInputElement).value?.toString() ?? "" }, "description")}
                        ></uui-input>
                    </div>
                </umb-property-layout>

                <umb-property-layout
                    label=${this.localize.term("uaiCopilotWorkspace_projectInstructionsLabel")}
                    description=${this.localize.term("uaiCopilotWorkspace_projectInstructionsHelp")}
                >
                    <div slot="editor">
                        <uui-textarea
                            .value=${this._model.instructions ?? ""}
                            rows="6"
                            @input=${(e: InputEvent) => this.#update({ instructions: (e.target as UUITextareaElement).value?.toString() ?? "" }, "instructions")}
                        ></uui-textarea>
                    </div>
                </umb-property-layout>

                <umb-property-layout label=${this.localize.term("uaiCopilotWorkspace_projectContextsLabel")}>
                    <div slot="editor">
                        <uai-context-picker
                            multiple
                            .value=${this._model.contextIds}
                            @change=${(e: Event) => this.#update({ contextIds: ((e.target as UaiContextPickerElement).value as string[] | undefined) ?? [] }, "contextIds")}
                        ></uai-context-picker>
                    </div>
                </umb-property-layout>

                <umb-property-layout label=${this.localize.term("uaiCopilotWorkspace_projectResourcesLabel")}>
                    <div slot="editor">
                        <uai-resource-list
                            .items=${this._model.resources}
                            @change=${(e: Event) => this.#update({ resources: [...(e.target as ResourceListElement).items] }, "resources")}
                        ></uai-resource-list>
                    </div>
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
            uui-input,
            uui-textarea {
                width: 100%;
            }
        `,
    ];
}

export default UaiCopilotWorkspaceProjectDetailsViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-project-details-view": UaiCopilotWorkspaceProjectDetailsViewElement;
    }
}
