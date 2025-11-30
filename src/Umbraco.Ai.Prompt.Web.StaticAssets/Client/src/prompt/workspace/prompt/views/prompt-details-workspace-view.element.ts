import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiPromptDetailModel } from "../../../types.js";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "../prompt-workspace.context-token.js";

const UAI_EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/**
 * Workspace view for Prompt details.
 * Displays content, description, tags, and status.
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
        this.#workspaceContext?.updateProperty("description", value || null);
    }

    #onContentChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLTextAreaElement).value;
        this.#workspaceContext?.updateProperty("content", value);
    }

    #onIsActiveChange(event: Event) {
        event.stopPropagation();
        const checked = (event.target as HTMLInputElement).checked;
        this.#workspaceContext?.updateProperty("isActive", checked);
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <div class="layout">
                <div class="main-column">${this.#renderLeftColumn()}</div>
                <div class="aside-column">${this.#renderRightColumn()}</div>
            </div>
        `;
    }

    #renderLeftColumn() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Prompt Configuration">
                <umb-property-layout label="Description" description="Brief description of this prompt">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Content" description="The prompt template text">
                    <uui-textarea
                        slot="editor"
                        .value=${this._model.content}
                        @input=${this.#onContentChange}
                        placeholder="Enter prompt content..."
                        rows="12"
                    ></uui-textarea>
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

    #renderRightColumn() {
        if (!this._model) return null;

        return html`
            <uui-box headline="Info">
                <umb-property-layout label="Id" orientation="vertical">
                    <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
                        ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
                        : this._model.unique}</div>
                </umb-property-layout>
                <umb-property-layout label="Active" orientation="vertical">
                    <uui-toggle
                        slot="editor"
                        ?checked=${this._model.isActive}
                        @change=${this.#onIsActiveChange}
                    ></uui-toggle>
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

            .layout {
                display: grid;
                grid-template-columns: 1fr 350px;
                gap: var(--uui-size-layout-1);
            }

            .main-column {
                min-width: 0;
            }

            .aside-column {
                min-width: 0;
            }

            @media (max-width: 1024px) {
                .layout {
                    grid-template-columns: 1fr;
                }
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

            umb-property-layout[orientation="vertical"]:not(:last-child) {
                padding-bottom: 0;
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
