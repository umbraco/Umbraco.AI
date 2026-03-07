import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand, UAI_EMPTY_GUID } from "@umbraco-ai/core";
import { UAI_ORCHESTRATION_WORKSPACE_CONTEXT } from "./orchestration-workspace.context-token.js";
import { UAI_ORCHESTRATION_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiOrchestrationDetailModel } from "../../types.js";
import { UAI_ORCHESTRATION_ROOT_WORKSPACE_PATH } from "../orchestration-root/paths.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { OrchestrationsService } from "../../../api/index.js";

@customElement("uai-orchestration-workspace-editor")
export class UaiOrchestrationWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_ORCHESTRATION_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiOrchestrationDetailModel;

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

        this.consumeContext(UAI_ORCHESTRATION_WORKSPACE_CONTEXT, (context) => {
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
        const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
        if (nameInput) this.addFormControlElement(nameInput as any);
    }

    async #checkAliasUniqueness(alias: string): Promise<void> {
        if (!alias) {
            this._aliasExists = false;
            return;
        }

        this._aliasCheckInProgress = true;

        const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
        nameInput?.setCustomValidity("Checking alias availability...");
        this.checkValidity();

        try {
            const { data } = await tryExecute(
                this,
                OrchestrationsService.orchestrationAliasExists({
                    path: { alias },
                    query: {
                        excludeId:
                            this._model?.unique !== UAI_EMPTY_GUID ? this._model?.unique : undefined,
                    },
                }),
            );

            this._aliasExists = data === true;

            const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");

            if (this._aliasExists) {
                nameInput?.setCustomValidity(this.localize.term("uaiValidation_aliasExists"));
            } else {
                nameInput?.setCustomValidity("");
            }

            this.checkValidity();
        } finally {
            this._aliasCheckInProgress = false;
        }
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const name = target.value.toString();

        if (this._aliasLocked && this._isNew) {
            const alias = this.#generateAlias(name);
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ name, alias }, "name-alias"),
            );

            this._aliasExists = false;
            if (this._aliasCheckTimeout) {
                clearTimeout(this._aliasCheckTimeout);
            }
            this._aliasCheckTimeout = window.setTimeout(() => {
                this.#checkAliasUniqueness(alias);
            }, 500);
        } else {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ name }, "name"),
            );
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const alias = target.value.toString();

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ alias }, "alias"),
        );

        this._aliasExists = false;

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
            new UaiPartialUpdateCommand<UaiOrchestrationDetailModel>({ isActive: e.detail.value }, "isActive"),
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_ORCHESTRATION_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button href=${UAI_ORCHESTRATION_ROOT_WORKSPACE_PATH} label="Back to Orchestrations" compact>
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
                            pattern="^[a-z0-9\\-]+$"
                            .requiredMessage=${this.localize.term("uaiValidation_required")}
                            .maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 100)}
                            .patternMessage=${this.localize.term("uaiValidation_aliasFormat")}
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
                    <a href=${UAI_ORCHESTRATION_ROOT_WORKSPACE_PATH}>Orchestrations</a>
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

export default UaiOrchestrationWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-orchestration-workspace-editor": UaiOrchestrationWorkspaceEditorElement;
    }
}
