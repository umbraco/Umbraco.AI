import { css, customElement, html, nothing, property, state } from "@umbraco-cms/backoffice/external/lit";
import type { PropertyValues } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import type { UUIInputElement, UUITextareaElement } from "@umbraco-cms/backoffice/external/uui";
import type { UaiContextPickerElement } from "@umbraco-ai/core";
import { UaiProjectRepository } from "../../../project/repository/project.repository.js";
import { UaiConversationRepository } from "../../../conversation/repository/conversation.repository.js";
import type { ContextResourceModel } from "../../../api/types.gen.js";
import {
    copilotWorkspaceConversationPath,
    UAI_COPILOT_WORKSPACE_SECTION_PATH,
} from "../../../paths.js";

/** Minimal structural type for the globally-registered (but not type-exported) `uai-resource-list`. */
interface ResourceListElement extends HTMLElement {
    items: ContextResourceModel[];
}

/**
 * Center-region view for a project: edits its shared instructions, attached context sets, and
 * resources — the context every conversation in the project inherits (injected server-side). Reuses
 * the core `<uai-context-picker>` and `<uai-resource-list>` (the same editors the AIContext workspace
 * uses). The router reuses this element across projects, so a changed `projectId` reloads.
 */
@customElement("uai-copilot-workspace-project-view")
export class UaiCopilotWorkspaceProjectViewElement extends UmbLitElement {
    #projectRepository = new UaiProjectRepository(this);
    #conversationRepository = new UaiConversationRepository(this);
    #requestToken = 0;

    #projectId?: string;

    @property({ type: String })
    get projectId(): string | undefined {
        return this.#projectId;
    }
    set projectId(value: string | undefined) {
        const previous = this.#projectId;
        this.#projectId = value;
        this.requestUpdate("projectId", previous);
    }

    @state() private _loading = false;
    @state() private _saving = false;
    @state() private _found = true;

    @state() private _name = "";
    @state() private _description = "";
    @state() private _instructions = "";
    @state() private _contextIds: string[] = [];
    @state() private _resources: ContextResourceModel[] = [];

    override willUpdate(changed: PropertyValues) {
        if (changed.has("projectId")) {
            void this.#load();
        }
    }

    async #load() {
        const id = this.#projectId;
        if (!id) return;
        const token = ++this.#requestToken;
        this._loading = true;

        const { data } = await this.#projectRepository.requestById(id);
        if (token !== this.#requestToken) return;

        if (!data) {
            this._found = false;
            this._loading = false;
            return;
        }
        this._found = true;
        this._name = data.name;
        this._description = data.description ?? "";
        this._instructions = data.instructions ?? "";
        this._contextIds = [...data.contextIds];
        this._resources = [...data.resources];
        this._loading = false;
    }

    async #save() {
        const id = this.#projectId;
        if (!id || this._saving) return;
        this._saving = true;
        await this.#projectRepository.update(id, {
            name: this._name.trim() || this.localize.term("uaiCopilotWorkspace_newProjectDefaultName"),
            description: this._description.trim() || null,
            instructions: this._instructions.trim() || null,
            contextIds: this._contextIds,
            resources: this._resources,
        });
        this._saving = false;
    }

    async #delete() {
        const id = this.#projectId;
        if (!id) return;
        await umbConfirmModal(this, {
            headline: this.localize.term("uaiCopilotWorkspace_projectDeleteConfirmTitle"),
            content: this.localize.term("uaiCopilotWorkspace_projectDeleteConfirmMessage"),
            color: "danger",
            confirmLabel: this.localize.term("uaiCopilotWorkspace_projectDelete"),
        });
        const { error } = await this.#projectRepository.delete(id);
        if (error) return;
        window.history.pushState({}, "", UAI_COPILOT_WORKSPACE_SECTION_PATH);
    }

    async #newChatInProject() {
        const id = this.#projectId;
        if (!id) return;
        const { data } = await this.#conversationRepository.create({ projectId: id });
        if (data?.id) {
            window.history.pushState({}, "", copilotWorkspaceConversationPath(data.id));
        }
    }

    render() {
        if (this._loading) {
            return html`<div class="pad"><uui-loader></uui-loader></div>`;
        }
        if (!this._found) {
            return html`<div class="pad"><p>${this.localize.term("uaiCopilotWorkspace_projectNotFound")}</p></div>`;
        }
        return html`
            <div class="pad">
                <div class="toolbar">
                    <uui-input
                        class="name"
                        .value=${this._name}
                        placeholder=${this.localize.term("uaiCopilotWorkspace_projectNamePlaceholder")}
                        label=${this.localize.term("uaiCopilotWorkspace_projectNameLabel")}
                        @input=${(e: InputEvent) => (this._name = (e.target as UUIInputElement).value?.toString() ?? "")}
                    ></uui-input>
                    <div class="actions">
                        <uui-button
                            look="secondary"
                            label=${this.localize.term("uaiCopilotWorkspace_projectNewChat")}
                            @click=${this.#newChatInProject}
                        >
                            <uui-icon name="icon-add"></uui-icon>
                            ${this.localize.term("uaiCopilotWorkspace_projectNewChat")}
                        </uui-button>
                        <uui-button
                            look="primary"
                            label=${this.localize.term("uaiCopilotWorkspace_projectSave")}
                            ?disabled=${this._saving}
                            @click=${this.#save}
                        >
                            ${this._saving ? html`<uui-loader-circle></uui-loader-circle>` : nothing}
                            ${this.localize.term("uaiCopilotWorkspace_projectSave")}
                        </uui-button>
                    </div>
                </div>

                <uui-box>
                    ${this.#renderField(
                        "uaiCopilotWorkspace_projectDescriptionLabel",
                        html`<uui-input
                            .value=${this._description}
                            @input=${(e: InputEvent) => (this._description = (e.target as UUIInputElement).value?.toString() ?? "")}
                        ></uui-input>`,
                    )}
                    ${this.#renderField(
                        "uaiCopilotWorkspace_projectInstructionsLabel",
                        html`<uui-textarea
                            .value=${this._instructions}
                            rows="5"
                            @input=${(e: InputEvent) => (this._instructions = (e.target as UUITextareaElement).value?.toString() ?? "")}
                        ></uui-textarea>`,
                        "uaiCopilotWorkspace_projectInstructionsHelp",
                    )}
                    ${this.#renderField(
                        "uaiCopilotWorkspace_projectContextsLabel",
                        html`<uai-context-picker
                            multiple
                            .value=${this._contextIds}
                            @change=${(e: Event) => (this._contextIds = ((e.target as UaiContextPickerElement).value as string[] | undefined) ?? [])}
                        ></uai-context-picker>`,
                    )}
                    ${this.#renderField(
                        "uaiCopilotWorkspace_projectResourcesLabel",
                        html`<uai-resource-list
                            .items=${this._resources}
                            @change=${(e: Event) => (this._resources = [...(e.target as ResourceListElement).items])}
                        ></uai-resource-list>`,
                    )}
                </uui-box>

                <div class="footer">
                    <uui-button
                        look="secondary"
                        color="danger"
                        label=${this.localize.term("uaiCopilotWorkspace_projectDelete")}
                        @click=${this.#delete}
                    >
                        <uui-icon name="icon-trash"></uui-icon>
                        ${this.localize.term("uaiCopilotWorkspace_projectDelete")}
                    </uui-button>
                </div>
            </div>
        `;
    }

    #renderField(labelKey: string, control: unknown, helpKey?: string) {
        return html`
            <div class="field">
                <label>${this.localize.term(labelKey)}</label>
                ${helpKey ? html`<small class="help">${this.localize.term(helpKey)}</small>` : nothing}
                ${control}
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
                height: 100%;
                overflow-y: auto;
            }
            .pad {
                padding: var(--uui-size-layout-1);
                max-width: 900px;
                margin: 0 auto;
            }
            .toolbar {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-4);
                margin-bottom: var(--uui-size-space-4);
            }
            .toolbar .name {
                flex: 1;
                font-size: 1.1rem;
            }
            .actions {
                display: flex;
                gap: var(--uui-size-space-2);
            }
            .field {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-1);
                margin-bottom: var(--uui-size-space-5);
            }
            .field label {
                font-weight: bold;
            }
            .field .help {
                color: var(--uui-color-text-alt);
                margin-bottom: var(--uui-size-space-1);
            }
            .field uui-input,
            .field uui-textarea {
                width: 100%;
            }
            .footer {
                margin-top: var(--uui-size-space-4);
            }
        `,
    ];
}

export default UaiCopilotWorkspaceProjectViewElement;
