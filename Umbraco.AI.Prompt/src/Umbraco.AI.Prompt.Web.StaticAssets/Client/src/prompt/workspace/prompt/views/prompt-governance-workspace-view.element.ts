import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiPromptDetailModel } from "../../../types.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

/**
 * Workspace view for Prompt governance settings.
 * Displays guardrail picker for prompt entities.
 */
@customElement("uai-prompt-governance-workspace-view")
export class UaiPromptGovernanceWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROMPT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiPromptDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROMPT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onGuardrailIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ guardrailIds: picker.value ?? [] }, "guardrailIds"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
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

export default UaiPromptGovernanceWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-governance-workspace-view": UaiPromptGovernanceWorkspaceViewElement;
    }
}
