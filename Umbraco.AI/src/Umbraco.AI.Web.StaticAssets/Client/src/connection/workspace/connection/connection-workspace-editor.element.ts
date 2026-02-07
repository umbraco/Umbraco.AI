import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import type { UUIButtonState } from "@umbraco-cms/backoffice/external/uui";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_CONNECTION_WORKSPACE_CONTEXT } from "./connection-workspace.context-token.js";
import { UAI_CONNECTION_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../connection-root/paths.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";
import { ConnectionsService } from "../../../api/sdk.gen.js";
import "../../../core/components/status-selector/status-selector.element.js";

@customElement("uai-connection-workspace-editor")
export class UaiConnectionWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_CONNECTION_WORKSPACE_CONTEXT.TYPE;
    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

    @state()
    private _model?: UaiConnectionDetailModel;

    @state()
    private _isNew?: boolean;

    @state()
    private _aliasLocked = true;

    @state()
    private _testButtonState?: UUIButtonState;

    @state()
    private _testButtonColor?: "default" | "positive" | "warning" | "danger" = "default";

    @state()
    private _aliasCheckInProgress = false;

    @state()
    private _aliasExists = false;

    private _aliasCheckTimeout?: number;

    constructor() {
        super();

        // Add custom validator for alias uniqueness
        this.addValidator(
            "customError",
            () => this.localize.term("uaiValidation_aliasExists"),
            () => this._aliasExists,
        );

        this.consumeContext(UAI_CONNECTION_WORKSPACE_CONTEXT, (context) => {
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

        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    protected override firstUpdated() {
        super.firstUpdated();
        // Register form control elements to enable HTML5 validation
        const nameInput = this.shadowRoot?.querySelector("#name");
        if (nameInput) this.addFormControlElement(nameInput);
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
                ConnectionsService.aliasExists({
                    path: { alias },
                    query: {
                        excludeId:
                            this._model?.unique !== UAI_EMPTY_GUID ? this._model?.unique : undefined,
                    },
                }),
            );

            this._aliasExists = data === true;

            // Trigger validation re-check
            this.checkValidity();
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
                new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ name, alias }, "name-alias"),
            );
        } else {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ name }, "name"),
            );
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const alias = target.value.toString();

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ alias }, "alias"),
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
            new UaiPartialUpdateCommand<UaiConnectionDetailModel>({ isActive: e.detail.value }, "isActive"),
        );
    }

    async #onTestConnection() {
        const unique = this._model?.unique;
        if (!unique || unique === UAI_EMPTY_GUID) return;

        this._testButtonState = "waiting";
        this._testButtonColor = "default";

        const { data, error } = await tryExecute(
            this,
            ConnectionsService.testConnection({ path: { connectionIdOrAlias: unique } }),
        );

        if (error || !data?.success) {
            this._testButtonState = "failed";
            this._testButtonColor = "danger";
            this.#notificationContext?.peek("danger", {
                data: { message: data?.errorMessage ?? this.localize.string("#uaiConnection_testConnectionFailed") },
            });
            this.#resetButtonState();
            return;
        }

        this._testButtonState = "success";
        this._testButtonColor = "positive";
        this.#notificationContext?.peek("positive", {
            data: { message: this.localize.string("#uaiConnection_testConnectionSuccess") },
        });
        this.#resetButtonState();
    }

    #resetButtonState() {
        setTimeout(() => {
            this._testButtonState = undefined;
            this._testButtonColor = "default";
        }, 2000);
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_CONNECTION_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH} label="Back to connections" compact>
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input="${this.#onNameChange}"
                        label="Name"
                        placeholder="Enter connection name"
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
                            label="Alias"
                            placeholder="Enter alias"
                            .value=${this._model.alias}
                            ?auto-width=${!!this._model.name}
                            ?locked=${this._aliasLocked}
                            ?readonly=${this._aliasLocked || !this._isNew}
                            @input=${this.#onAliasChange}
                            @lock-change=${this.#onToggleAliasLock}
                            required
                            maxlength="100"
                            pattern="^[a-z0-9-]+$"
                            .requiredMessage=${this.localize.term("uaiValidation_required")}
                            .maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 100)}
                            .patternMessage=${this.localize.term("uaiValidation_aliasFormat")}
                            ${umbBindToValidation(this, "$.alias", this._model.alias)}
                        >
                            ${this._aliasCheckInProgress
                                ? html`<uui-loader slot="append"></uui-loader>`
                                : nothing}
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
                ${when(
                    !this._isNew && this._model,
                    () => html`
                        <uui-button
                            slot="actions"
                            label=${this.localize.string("#uaiConnection_testConnection")}
                            look="default"
                            .color=${this._testButtonColor}
                            .state=${this._testButtonState}
                            @click=${this.#onTestConnection}
                        >
                            ${this.localize.string("#uaiConnection_testConnection")}
                        </uui-button>
                    `,
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}>Connections</a>
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

export default UaiConnectionWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-workspace-editor": UaiConnectionWorkspaceEditorElement;
    }
}
