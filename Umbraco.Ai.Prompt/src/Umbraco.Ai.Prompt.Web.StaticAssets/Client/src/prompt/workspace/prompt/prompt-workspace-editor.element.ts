import { css, html, customElement, state, when } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import { UAI_PROMPT_WORKSPACE_CONTEXT } from "./prompt-workspace.context-token.js";
import { UAI_PROMPT_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiPromptDetailModel } from "../../types.js";
import { UAI_PROMPT_ROOT_WORKSPACE_PATH } from "../prompt-root/paths.js";

@customElement("uai-prompt-workspace-editor")
export class UaiPromptWorkspaceEditorElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROMPT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiPromptDetailModel;

    @state()
    private _isNew?: boolean;

    @state()
    private _aliasLocked = true;

    constructor() {
        super();

        this.consumeContext(UAI_PROMPT_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.#workspaceContext = context;
            this.observe(context.model, (model) => {
                this._model = model;
            });
            this.observe(context.isNew, (isNew) => {
                this._isNew = isNew;
                if (isNew) {
                    requestAnimationFrame(() => {
                        (this.shadowRoot?.querySelector("#name") as HTMLElement)?.focus();
                    });
                }
            });
        });
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const name = target.value.toString();

        // If alias is locked and creating new, generate alias from name
        if (this._aliasLocked && this._isNew) {
            const alias = this.#generateAlias(name);
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiPromptDetailModel>({ name, alias }, "name-alias")
            );
        } else {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiPromptDetailModel>({ name }, "name")
            );
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiPromptDetailModel>({ alias: target.value.toString() }, "alias")
        );
    }

    #onToggleAliasLock() {
        this._aliasLocked = !this._aliasLocked;
    }

    #generateAlias(name: string): string {
        return name
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, "-")
            .replace(/^-|-$/g, "");
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_PROMPT_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button
                        href=${UAI_PROMPT_ROOT_WORKSPACE_PATH}
                        label="Back to prompts"
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${this.#onNameChange}"
                        label="Name"
                        placeholder="Enter prompt name"
                    >
                        <uui-input-lock
                            slot="append"
                            id="alias"
                            name="alias"
                            label="Alias"
                            placeholder="Enter alias"
                            .value=${this._model.alias}
                            ?auto-width=${!!this._model.name}
                            ?locked=${this._aliasLocked}
                            @input=${this.#onAliasChange}
                            @lock-change=${this.#onToggleAliasLock}
                        ></uui-input-lock>
                    </uui-input>
                </div>

                ${when(
                    !this._isNew && this._model,
                    () => html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_PROMPT_ROOT_WORKSPACE_PATH}>Prompts</a>
                    / ${this._model.name || "Untitled"}
                </div>
            </umb-workspace-editor>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }

            #header {
                display: flex;
                flex: 1 1 auto;
                gap: var(--uui-size-space-2);
            }

            #name {
                width: 100%;
                flex: 1 1 auto;
                align-items: center;
            }

            #footer {
                padding: 0 var(--uui-size-layout-1);
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

export default UaiPromptWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-workspace-editor": UaiPromptWorkspaceEditorElement;
    }
}
