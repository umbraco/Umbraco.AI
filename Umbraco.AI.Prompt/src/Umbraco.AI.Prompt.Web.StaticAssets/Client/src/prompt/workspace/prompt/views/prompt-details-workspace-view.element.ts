import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import "@umbraco-ai/core";
import "@umbraco-cms/backoffice/markdown-editor";
import type { UaiPromptDetailModel } from "../../../types.js";
import type { UaiPromptScope, UaiScopeRule } from "../../../property-actions/types.js";
import { TEXT_BASED_PROPERTY_EDITOR_UIS } from "../../../property-actions/constants.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

/**
 * Creates a default scope with one allow rule for all text-based editors.
 */
function createDefaultScope(): UaiPromptScope {
    return {
        allowRules: [{
            propertyEditorUiAliases: [...TEXT_BASED_PROPERTY_EDITOR_UIS],
            propertyAliases: null,
            contentTypeAliases: null,
        }],
        denyRules: [],
    };
}

/**
 * Workspace view for Prompt details.
 * Displays instructions, description, scope configuration, and tags.
 */
@customElement("uai-prompt-details-workspace-view")
export class UaiPromptDetailsWorkspaceViewElement extends UmbLitElement {
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

    #onDescriptionChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ description: value || null }, "description")
        );
    }

    #onInstructionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLTextAreaElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ instructions: value }, "instructions")
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ profileId }, "profileId")
        );
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>(
                { contextIds: picker.value ?? [] },
                "contextIds"
            )
        );
    }

    #onIncludeEntityContextChange(event: Event) {
        event.stopPropagation();
        const checked = (event.target as HTMLInputElement).checked;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ includeEntityContext: checked }, "includeEntityContext")
        );
    }

    #updateScope(scope: UaiPromptScope) {
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ scope }, "scope")
        );
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
            <uui-box headline="General">
                <umb-property-layout label="AI Profile" description="Select a profile or leave empty to use the default Chat profile from Settings">
                    <uai-profile-picker
                        slot="editor"
                        .value=${this._model.profileId ?? undefined}
                        @change=${this.#onProfileChange}
                    ></uai-profile-picker>
                </umb-property-layout>

                <umb-property-layout label="Description" description="Brief description of this prompt">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Contexts" description="Predefined contexts to include when executing this prompt">
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${this._model.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>

                <umb-property-layout label="Include Entity Context" description="When enabled, all entity properties are formatted as markdown and injected as a system message. Variable replacement ({{property}}) works regardless of this setting.">
                    <uui-toggle
                        slot="editor"
                        ?checked=${this._model.includeEntityContext}
                        @change=${this.#onIncludeEntityContextChange}
                    ></uui-toggle>
                </umb-property-layout>

                <umb-property-layout label="Instructions" description="The prompt instructions template">
                    <umb-input-markdown
                        slot="editor"
                        .value=${this._model.instructions ?? ""}
                        @change=${this.#onInstructionsChange}
                    ></umb-input-markdown>
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Scope">
                <umb-property-layout
                    label="Allow"
                    description="Prompt is allowed where ANY rule matches (OR logic between rules)"
                >
                    <uai-scope-rules-editor
                        slot="editor"
                        .rules=${scope.allowRules}
                        addButtonLabel="Add Allow Rule"
                        @rules-change=${this.#onAllowRulesChange}
                    ></uai-scope-rules-editor>
                </umb-property-layout>

                <umb-property-layout
                    label="Deny"
                    description="Prompt is denied where ANY rule matches (overrides allow rules)"
                >
                    <uai-scope-rules-editor
                        slot="editor"
                        .rules=${scope.denyRules}
                        addButtonLabel="Add Deny Rule"
                        @rules-change=${this.#onDenyRulesChange}
                    ></uai-scope-rules-editor>
                </umb-property-layout>
            </uui-box>

            ${this._model.tags.length > 0 ? html`
                <uui-box headline="Tags">
                    <div class="tags-container">
                        ${this._model.tags.map((tag) => html`<uui-tag>${tag}</uui-tag>`)}
                    </div>
                </uui-box>
            ` : nothing}
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
            uui-textarea {
                width: 100%;
            }

            .tags-container {
                display: flex;
                flex-wrap: wrap;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-3) 0;
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

export default UaiPromptDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-details-workspace-view": UaiPromptDetailsWorkspaceViewElement;
    }
}
