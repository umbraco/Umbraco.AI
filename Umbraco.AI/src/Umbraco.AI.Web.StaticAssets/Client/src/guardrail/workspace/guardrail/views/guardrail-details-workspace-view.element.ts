import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiGuardrailDetailModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_GUARDRAIL_WORKSPACE_CONTEXT } from "../guardrail-workspace.context-token.js";
import type { UaiGuardrailRuleConfigBuilderElement } from "../../../components/rule-config-builder/rule-config-builder.element.js";

/**
 * Workspace view for Guardrail details.
 * Displays and manages guardrail rules.
 */
@customElement("uai-guardrail-details-workspace-view")
export class UaiGuardrailDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_GUARDRAIL_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiGuardrailDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_GUARDRAIL_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onRulesChange(e: Event) {
        if (!this._model) return;

        const builder = e.target as UaiGuardrailRuleConfigBuilderElement;
        const rules = builder.rules;

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiGuardrailDetailModel>({ rules }, "update-rules"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Rules">
                <umb-property-layout
                    label="Rules"
                    description="Define rules that evaluate AI requests and responses for safety compliance."
                >
                    <div slot="editor">
                        <uai-guardrail-rule-config-builder
                            .rules=${this._model.rules}
                            @change=${this.#onRulesChange}
                        >
                        </uai-guardrail-rule-config-builder>
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
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }
        `,
    ];
}

export default UaiGuardrailDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-guardrail-details-workspace-view": UaiGuardrailDetailsWorkspaceViewElement;
    }
}
