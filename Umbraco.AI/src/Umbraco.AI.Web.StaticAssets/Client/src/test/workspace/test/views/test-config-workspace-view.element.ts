import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../test-workspace.context-token.js";
import type { UaiTestDetailModel, UaiTestVariation } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/command/implement/partial-update.command.js";

@customElement("umbraco-ai-test-config-workspace-view")
export class UmbracoAITestConfigWorkspaceViewElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_TEST_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiTestDetailModel;

    constructor() {
        super();

        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.#workspaceContext = context;
            this.observe(context.model, (model) => {
                this._model = model;
            });
        });
    }

    #onVariationsChange(e: UmbChangeEvent) {
        e.stopPropagation();
        const builder = e.target as HTMLElement & { variations?: UaiTestVariation[] };
        const variations = builder.variations ?? [];
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiTestDetailModel>({ variations }, "variations"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Variations" class="variations-box">
                <umb-property-layout
                    label="Variations"
                    description="Configure variations to run alongside the default configuration"
                    mandatory>
                    <uai-test-variation-config-builder
                        slot="editor"
                        .variations=${this._model.variations}
                        .testFeatureId=${this._model.testFeatureId}
                        @change=${this.#onVariationsChange}
                    ></uai-test-variation-config-builder>
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

            uui-select,
            uui-input,
            uui-textarea {
                width: 100%;
            }
        `,
    ];
}

export default UmbracoAITestConfigWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-config-workspace-view": UmbracoAITestConfigWorkspaceViewElement;
    }
}
