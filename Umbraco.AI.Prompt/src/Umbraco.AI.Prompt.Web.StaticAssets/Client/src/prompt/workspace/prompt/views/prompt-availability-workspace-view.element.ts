import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiPromptDetailModel } from "../../../types.js";
import type { UaiPromptScope, UaiScopeRule } from "../../../property-actions/types.js";
import { TEXT_BASED_PROPERTY_EDITOR_UIS } from "../../../property-actions/constants.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

/**
 * Creates a default scope with one allow rule for all text-based editors.
 */
function createDefaultScope(): UaiPromptScope {
    return {
        allowRules: [
            {
                propertyEditorUiAliases: [...TEXT_BASED_PROPERTY_EDITOR_UIS],
                propertyAliases: null,
                contentTypeAliases: null,
            },
        ],
        denyRules: [],
    };
}

/**
 * Workspace view for Prompt availability configuration.
 * Controls where the prompt action appears (property editor scope rules).
 */
@customElement("uai-prompt-availability-workspace-view")
export class UaiPromptAvailabilityWorkspaceViewElement extends UmbLitElement {
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

    #getScope(): UaiPromptScope {
        return this._model?.scope ?? createDefaultScope();
    }

    #updateScope(scope: UaiPromptScope) {
        this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand<UaiPromptDetailModel>({ scope }, "scope"));
    }

    #onAllowRulesChange(event: CustomEvent<UaiScopeRule[]>) {
        event.stopPropagation();
        const scope = this.#getScope();
        this.#updateScope({
            ...scope,
            allowRules: event.detail,
        });
    }

    #onDenyRulesChange(event: CustomEvent<UaiScopeRule[]>) {
        event.stopPropagation();
        const scope = this.#getScope();
        this.#updateScope({
            ...scope,
            denyRules: event.detail,
        });
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        const scope = this.#getScope();

        return html`
            <uui-box headline="Scope">
                <div class="scope-description">
                    <p>
                        Control where this prompt action appears in the backoffice based on property editor, property,
                        and content type. Leave empty to show the prompt everywhere.
                    </p>
                </div>

                <umb-property-layout
                    label="Allow Rules"
                    description="Prompt appears where ANY rule matches (OR logic between rules)"
                >
                    <uai-prompt-scope-rules-editor
                        slot="editor"
                        .rules=${scope.allowRules}
                        addButtonLabel="Add Allow Rule"
                        @rules-change=${this.#onAllowRulesChange}
                    ></uai-prompt-scope-rules-editor>
                </umb-property-layout>

                <umb-property-layout
                    label="Deny Rules"
                    description="Prompt is hidden where ANY rule matches (overrides allow rules)"
                >
                    <uai-prompt-scope-rules-editor
                        slot="editor"
                        .rules=${scope.denyRules}
                        addButtonLabel="Add Deny Rule"
                        @rules-change=${this.#onDenyRulesChange}
                    ></uai-prompt-scope-rules-editor>
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

            .scope-description {
                padding: var(--uui-size-space-4) 0;
                color: var(--uui-color-text-alt);
            }

            .scope-description p {
                margin: 0 0 var(--uui-size-space-2);
            }
        `,
    ];
}

export default UaiPromptAvailabilityWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-availability-workspace-view": UaiPromptAvailabilityWorkspaceViewElement;
    }
}
