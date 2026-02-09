import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand, UAI_EMPTY_GUID } from "@umbraco-ai/core";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "./agent-workspace.context-token.js";
import { UAI_AGENT_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiAgentDetailModel } from "../../types.js";
import { UAI_AGENT_ROOT_WORKSPACE_PATH } from "../agent-root/paths.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { AgentsService } from "../../../api/index.js";

@customElement("uai-agent-workspace-editor")
export class UaiAgentWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_AGENT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiAgentDetailModel;

    @state()
    private _isNew?: boolean;

    @state()
    private _aliasLocked = true;

    @state()
    private _aliasCheckInProgress = false;

    @state()
    private _aliasExists = false;

    private _aliasCheckTimeout?: number;

    constructor() {
        super();

        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
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

    protected override firstUpdated(_changedProperties: any) {
        super.firstUpdated(_changedProperties);
        // Register form control elements to enable HTML5 validation
        const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
        if (nameInput) this.addFormControlElement(nameInput as any);
    }

    async #checkAliasUniqueness(alias: string): Promise<void> {
        if (!alias) {
            this._aliasExists = false;
            return;
        }

        this._aliasCheckInProgress = true;
        try {
            const { data } = await tryExecute(
                this,
                AgentsService.agentAliasExists({
                    path: { alias },
                    query: {
                        excludeId: this._model?.unique !== UAI_EMPTY_GUID ? this._model?.unique : undefined,
                    },
                }),
            );

            this._aliasExists = data === true;

            // Add/remove validation message on the workspace validation context
            if (this._aliasExists) {
                this.#workspaceContext?.validation.messages.addMessage(
                    'error',
                    '$.alias',
                    this.localize.term('uaiValidation_aliasExists'),
                    'alias-uniqueness' // unique key for this validation message
                );
            } else {
                this.#workspaceContext?.validation.messages.removeMessageByKey('alias-uniqueness');
            }
        } finally {
            this._aliasCheckInProgress = false;
        }
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const name = target.value.toString();

        // If alias is locked and creating new, generate alias from name
        if (this._aliasLocked && this._isNew) {
            const alias = this.#generateAlias(name);
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiAgentDetailModel>({ name, alias }, "name-alias"),
            );

            // Trigger alias uniqueness check for auto-generated alias
            this._aliasExists = false;
            if (this._aliasCheckTimeout) {
                clearTimeout(this._aliasCheckTimeout);
            }
            this._aliasCheckTimeout = window.setTimeout(() => {
                this.#checkAliasUniqueness(alias);
            }, 500);
        } else {
            this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand<UaiAgentDetailModel>({ name }, "name"));
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const alias = target.value.toString();

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ alias }, "alias"),
        );

        // Reset uniqueness flag when user changes value
        this._aliasExists = false;

        // Debounced uniqueness check
        if (this._aliasCheckTimeout) {
            clearTimeout(this._aliasCheckTimeout);
        }
        this._aliasCheckTimeout = window.setTimeout(() => {
            this.#checkAliasUniqueness(alias);
        }, 500);
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

    #onActiveChange(e: CustomEvent<{ value: boolean }>) {
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ isActive: e.detail.value }, "isActive"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_AGENT_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button href=${UAI_AGENT_ROOT_WORKSPACE_PATH} label="Back to Agents" compact>
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${this.#onNameChange}"
                        label=${this.localize.term("uaiLabels_name")}
                        placeholder=${this.localize.term("uaiPlaceholders_enterName")}
                        required
                        maxlength="255"
                        .requiredMessage=${this.localize.term("uaiValidation_required")}
                        .maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 255)}
                        ${umbBindToValidation(this, "$.name", this._model.name)}
                    >
                        <uui-input-lock
                            slot="append"
                            id="alias"
                            name="alias"
                            label=${this.localize.term("uaiLabels_alias")}
                            .value=${this._model.alias}
                            ?auto-width=${!!this._model.name}
                            ?locked=${this._aliasLocked}
                            ?readonly=${this._aliasLocked || !this._isNew}
                            @input=${this.#onAliasChange}
                            @lock-change=${this.#onToggleAliasLock}
                            required
                            maxlength="100"
                            pattern="^[a-zA-Z0-9_-]+$"
                            .requiredMessage=${this.localize.term("uaiValidation_required")}
                            .maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 100)}
                            .patternMessage=${this.localize.term("uaiValidation_aliasFormat")}
                            ?error=${this._aliasExists}
                            .errorMessage=${this._aliasExists ? this.localize.term("uaiValidation_aliasExists") : ""}
                            ${umbBindToValidation(this, "$.alias", this._model.alias)}
                        >
                            ${this._aliasCheckInProgress ? html`<uui-loader slot="append"></uui-loader>` : nothing}
                        </uui-input-lock>
                    </uui-input>

                    <uai-status-selector
                        .value=${this._model.isActive}
                        @change=${this.#onActiveChange}
                    ></uai-status-selector>
                </div>

                ${when(
                    !this._isNew && this._model,
                    () =>
                        html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`,
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_AGENT_ROOT_WORKSPACE_PATH}>Agents</a>
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
                gap: var(--uui-size-space-3);
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

export default UaiAgentWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-workspace-editor": UaiAgentWorkspaceEditorElement;
    }
}
