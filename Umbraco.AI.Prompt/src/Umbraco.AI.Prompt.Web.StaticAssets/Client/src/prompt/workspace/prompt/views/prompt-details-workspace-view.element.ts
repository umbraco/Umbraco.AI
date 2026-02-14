import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbBindToValidation } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import "@umbraco-ai/core";
import "@umbraco-cms/backoffice/markdown-editor";
import type { UaiPromptDetailModel } from "../../../types.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

/**
 * Workspace view for Prompt settings.
 * Configures prompt behavior: profile, description, contexts, and instructions.
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

    #onDescriptionChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ description: value || null }, "description"),
        );
    }

    #onInstructionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLTextAreaElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ instructions: value }, "instructions"),
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ profileId }, "profileId"),
        );
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ contextIds: picker.value ?? [] }, "contextIds"),
        );
    }

    #onIncludeEntityContextChange(event: Event) {
        event.stopPropagation();
        const checked = (event.target as HTMLInputElement).checked;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>(
                { includeEntityContext: checked },
                "includeEntityContext",
            ),
        );
    }

    #onResultTypeChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const select = event.target as HTMLElement & { value: string };
        const resultType = select.value;

        let optionCount: number;
        switch (resultType) {
            case "informational":
                optionCount = 0;
                break;
            case "single":
                optionCount = 1;
                break;
            case "multiple":
                // Default to 2 when switching to multiple
                optionCount = this._model?.optionCount && this._model.optionCount >= 2 ? this._model.optionCount : 2;
                break;
            default:
                optionCount = 1;
        }

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ optionCount }, "optionCount"),
        );
    }

    #onOptionCountChange(event: Event) {
        event.stopPropagation();
        const input = event.target as HTMLInputElement;
        const optionCount = Math.max(2, parseInt(input.value) || 2);
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ optionCount }, "optionCount"),
        );
    }

    #getResultType(): string {
        const count = this._model?.optionCount ?? 1;
        if (count === 0) return "informational";
        if (count === 1) return "single";
        return "multiple";
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout
                    label="AI Profile"
                    description="Select a profile or leave empty to use the default Chat profile from Settings"
                >
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

                <umb-property-layout
                    label="Contexts"
                    description="Predefined contexts to include when executing this prompt"
                >
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${this._model.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>

                <umb-property-layout
                    label="Include Entity Context"
                    description="When enabled, all entity properties are formatted as markdown and injected as a system message. Variable replacement ({{property}}) works regardless of this setting."
                >
                    <uui-toggle
                        slot="editor"
                        ?checked=${this._model.includeEntityContext}
                        @change=${this.#onIncludeEntityContextChange}
                    ></uui-toggle>
                </umb-property-layout>

                <umb-property-layout label="Instructions" description="The prompt instructions template" mandatory>
                    <umb-input-markdown
                        slot="editor"
                        .value=${this._model.instructions ?? ""}
                        @change=${this.#onInstructionsChange}
                        required
                        ${umbBindToValidation(this, "$.instructions", this._model.instructions)}
                    ></umb-input-markdown>
                </umb-property-layout>

                <umb-property-layout
                    label="Result Type"
                    description="How the AI response should be structured for user interaction"
                >
                    <uui-select
                        slot="editor"
                        .value=${this.#getResultType()}
                        @change=${this.#onResultTypeChange}
                    >
                        <uui-select-option value="informational">Informational</uui-select-option>
                        <uui-select-option value="single">Single Option</uui-select-option>
                        <uui-select-option value="multiple">Multiple Options</uui-select-option>
                    </uui-select>
                    <div slot="description" style="margin-top: var(--uui-size-space-2);">
                        <ul style="margin: 0; padding-left: var(--uui-size-space-5); list-style: disc;">
                            <li><strong>Informational:</strong> Display only, no value insertion</li>
                            <li><strong>Single Option:</strong> One result with direct insertion</li>
                            <li><strong>Multiple Options:</strong> User selects from AI-generated options</li>
                        </ul>
                    </div>
                </umb-property-layout>

                ${this.#getResultType() === "multiple"
                    ? html`
                          <umb-property-layout
                              label="Number of Options"
                              description="How many options the AI should generate (minimum 2)"
                          >
                              <uui-input
                                  slot="editor"
                                  type="number"
                                  min="2"
                                  max="10"
                                  step="1"
                                  .value=${this._model.optionCount?.toString() ?? "2"}
                                  @change=${this.#onOptionCountChange}
                              ></uui-input>
                          </umb-property-layout>
                      `
                    : nothing}
            </uui-box>

            ${this._model.tags.length > 0
                ? html`
                      <uui-box headline="Tags">
                          <div class="tags-container">
                              ${this._model.tags.map((tag) => html`<uui-tag>${tag}</uui-tag>`)}
                          </div>
                      </uui-box>
                  `
                : nothing}
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
