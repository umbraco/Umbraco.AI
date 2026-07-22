import { css, html, customElement, property, state, when } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UUIInputElement, UUIInputEvent } from "@umbraco-cms/backoffice/external/uui";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation, UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UaiPartialUpdateCommand, UaiEntityDeletedRedirectController } from "@umbraco-ai/core";
import { UaiProjectWorkspaceContext } from "./project-workspace.context.js";
import { UAI_PROJECT_WORKSPACE_ALIAS } from "../../constants.js";
import type { UaiProjectDetailModel } from "../types.js";
import { UAI_EMPTY_GUID } from "../types.js";
import { copilotWorkspaceProjectPath, UAI_COPILOT_WORKSPACE_SECTION_PATH } from "../../paths.js";

// The workspace views + Save action + Delete are contributed via manifests; importing them keeps
// their tabs available even though this element mounts the workspace context directly.
import "./views/project-details-workspace-view.element.js";
import "./views/project-info-workspace-view.element.js";

/**
 * Hosts the project entity workspace inside the section shell. It owns + provides the
 * {@link UaiProjectWorkspaceContext} (so `<umb-workspace-editor>`, the workspace views, and the Save
 * action resolve it) and renders the standard workspace chrome: a header with the name, the tab
 * strip (Details / Info), the Save action bar, a ⋯ entity-action menu, and a breadcrumb footer.
 *
 * Driven by the shell router: `create` scaffolds a new project; `projectId` loads an existing one.
 */
@customElement("uai-copilot-workspace-project-workspace-editor")
export class UaiCopilotWorkspaceProjectWorkspaceEditorElement extends UmbFormControlMixin(UmbLitElement) {
    #context = new UaiProjectWorkspaceContext(this);
    #didNavigateForCreate = false;

    @property({ type: Boolean })
    create = false;

    @property({ type: String })
    projectId?: string;

    @state() private _model?: UaiProjectDetailModel;
    @state() private _isNew?: boolean;

    constructor() {
        super();
        this.#context.defaultName = this.localize.term("uaiCopilotWorkspace_newProjectDefaultName");

        this.observe(this.#context.model, (model) => {
            this._model = model;
        });
        this.observe(this.#context.isNew, (isNew) => {
            this._isNew = isNew;
            if (isNew) {
                requestAnimationFrame(() => (this.shadowRoot?.querySelector("#name") as HTMLElement)?.focus());
            }
        });
        // On first successful save of a new project, reflect its real id in the URL so reload works.
        this.observe(this.#context.unique, (unique) => {
            if (this.create && !this.#didNavigateForCreate && unique && unique !== UAI_EMPTY_GUID) {
                this.#didNavigateForCreate = true;
                window.history.pushState({}, "", copilotWorkspaceProjectPath(unique));
            }
        });

        // Redirect back to the section root when this project is deleted (from the ⋯ menu).
        new UaiEntityDeletedRedirectController(this, {
            getUnique: () => this.#context.getUnique(),
            getEntityType: () => this.#context.getEntityType(),
            collectionPath: UAI_COPILOT_WORKSPACE_SECTION_PATH,
        });
    }

    override willUpdate(changed: PropertyValues) {
        super.willUpdate(changed);
        if (changed.has("create") && this.create) {
            this.#context.scaffold();
        }
        if (changed.has("projectId") && this.projectId) {
            void this.#context.load(this.projectId);
        }
    }

    override firstUpdated(changed: PropertyValues) {
        super.firstUpdated(changed);
        const nameInput = this.shadowRoot?.querySelector<UUIInputElement>("#name");
        if (nameInput) this.addFormControlElement(nameInput);
    }

    #onNameChange(event: UUIInputEvent) {
        event.stopPropagation();
        const name = (event.composedPath()[0] as UUIInputElement).value.toString();
        this.#context.handleCommand(new UaiPartialUpdateCommand<UaiProjectDetailModel>({ name }, "name"));
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;
        return html`
            <umb-workspace-editor alias=${UAI_PROJECT_WORKSPACE_ALIAS}>
                <div id="header" slot="header">
                    <uui-button
                        href=${UAI_COPILOT_WORKSPACE_SECTION_PATH}
                        label=${this.localize.term("uaiCopilotWorkspace_projectBack")}
                        compact
                    >
                        <uui-icon name="icon-arrow-left"></uui-icon>
                    </uui-button>
                    <uui-input
                        id="name"
                        .value=${this._model.name}
                        @input=${this.#onNameChange}
                        label=${this.localize.term("uaiCopilotWorkspace_projectNameLabel")}
                        placeholder=${this.localize.term("uaiCopilotWorkspace_projectNamePlaceholder")}
                        required
                        maxlength="255"
                        ${umbBindToValidation(this, "$.name", this._model.name)}
                    ></uui-input>
                </div>

                ${when(
                    !this._isNew,
                    () => html`<umb-workspace-entity-action-menu slot="action-menu"></umb-workspace-entity-action-menu>`,
                )}

                <div slot="footer-info" id="footer">
                    <a href=${UAI_COPILOT_WORKSPACE_SECTION_PATH}>${this.localize.term("uaiCopilotWorkspace_sectionLabel")}</a>
                    / ${this._model.name || this.localize.term("uaiCopilotWorkspace_newProjectDefaultName")}
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
                align-items: center;
            }
            #name {
                width: 100%;
                flex: 1 1 auto;
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

export default UaiCopilotWorkspaceProjectWorkspaceEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-copilot-workspace-project-workspace-editor": UaiCopilotWorkspaceProjectWorkspaceEditorElement;
    }
}
