import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "./profile-workspace.context-token.js";
import { UAI_PROFILE_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiProfileDetailModel } from "../../types.js";
import { UaiPartialUpdateCommand } from "../../../core/command/implement/partial-update.command.js";
import { UAI_PROFILE_ROOT_WORKSPACE_PATH } from "../profile-root/paths.js";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ProfilesService } from "../../../api/index.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";

@customElement("uai-profile-workspace-editor")
export class UaiProfileWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiProfileDetailModel;

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

        // ONLY custom validator: Alias uniqueness (async business logic)
        this.addValidator(
            "customError",
            () => this.localize.term("uaiValidation_aliasExists"),
            () => this._aliasExists,
        );

        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
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
                ProfilesService.profileAliasExists({
                    path: { alias },
                    query: {
                        excludeId: this._model?.unique !== UAI_EMPTY_GUID ? this._model?.unique : undefined,
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
                new UaiPartialUpdateCommand<UaiProfileDetailModel>({ name, alias }, "name-alias"),
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
            this.#workspaceContext?.handleCommand(new UaiPartialUpdateCommand<UaiProfileDetailModel>({ name }, "name"));
        }
    }

    #onAliasChange(event: UUIInputEvent) {
        event.stopPropagation();
        const target = event.composedPath()[0] as UUIInputElement;
        const alias = target.value.toString();

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ alias }, "alias"),
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

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <umb-workspace-editor alias="${UAI_PROFILE_WORKSPACE_ALIAS}">
                <div id="header" slot="header">
                    <uui-button href=${UAI_PROFILE_ROOT_WORKSPACE_PATH} label="Back to profiles" compact>
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
                            pattern="^[a-z0-9-]+$"
                            .requiredMessage=${this.localize.term("uaiValidation_required")}
                            .maxlengthMessage=${this.localize.term("uaiValidation_maxLength", 100)}
                            .patternMessage=${this.localize.term("uaiValidation_aliasFormat")}
                            ${umbBindToValidation(this, "$.alias", this._model.alias)}
                        >
                            ${this._aliasCheckInProgress ? html`<uui-loader slot="append"></uui-loader>` : nothing}
                        </uui-input-lock>
                    </uui-input>
                </div>

                ${when(
                    !this._isNew && this._model,
                    () =>
                        html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`,
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_PROFILE_ROOT_WORKSPACE_PATH}>Profiles</a>
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

export default UaiProfileWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-workspace-editor": UaiProfileWorkspaceEditorElement;
    }
}
