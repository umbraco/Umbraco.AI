import { css, html, customElement, state, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiContextDetailModel, UaiContextResourceModel } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_CONTEXT_WORKSPACE_CONTEXT } from "../context-workspace.context-token.js";

/**
 * Workspace view for Context details.
 * Displays and manages context resources.
 */
@customElement("uai-context-details-workspace-view")
export class UaiContextDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_CONTEXT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiContextDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_CONTEXT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onAddResource() {
        if (!this._model) return;

        const newResource: UaiContextResourceModel = {
            id: crypto.randomUUID(),
            resourceTypeId: "text",
            name: "New Resource",
            description: null,
            sortOrder: this._model.resources.length,
            data: JSON.stringify({ content: "" }),
            injectionMode: "Always",
        };

        const resources = [...this._model.resources, newResource];
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "add-resource")
        );
    }

    #onRemoveResource(resourceId: string) {
        if (!this._model) return;

        const resources = this._model.resources.filter((r) => r.id !== resourceId);
        // Update sort orders
        resources.forEach((r, idx) => (r.sortOrder = idx));

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "remove-resource")
        );
    }

    #onResourceNameChange(resourceId: string, name: string) {
        if (!this._model) return;

        const resources = this._model.resources.map((r) =>
            r.id === resourceId ? { ...r, name } : r
        );
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "update-resource-name")
        );
    }

    #onResourceDataChange(resourceId: string, content: string) {
        if (!this._model) return;

        const resources = this._model.resources.map((r) =>
            r.id === resourceId ? { ...r, data: JSON.stringify({ content }) } : r
        );
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "update-resource-data")
        );
    }

    #onResourceInjectionModeChange(resourceId: string, mode: "Always" | "OnDemand") {
        if (!this._model) return;

        const resources = this._model.resources.map((r) =>
            r.id === resourceId ? { ...r, injectionMode: mode } : r
        );
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiContextDetailModel>({ resources }, "update-resource-mode")
        );
    }

    #getResourceContent(resource: UaiContextResourceModel): string {
        try {
            const data = JSON.parse(resource.data);
            return data.content ?? "";
        } catch {
            return resource.data;
        }
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Resources" id="resources-box">
                <uui-button
                    slot="header-actions"
                    label="Add Resource"
                    look="outline"
                    @click=${this.#onAddResource}
                >
                    <uui-icon name="icon-add"></uui-icon>
                    Add Resource
                </uui-button>

                ${this._model.resources.length === 0
                    ? html`
                          <div class="empty-state">
                              <p>No resources added yet.</p>
                              <p>Resources provide contextual information to AI operations.</p>
                          </div>
                      `
                    : html`
                          <div class="resource-list">
                              ${repeat(
                                  this._model.resources,
                                  (r) => r.id,
                                  (resource) => this.#renderResource(resource)
                              )}
                          </div>
                      `}
            </uui-box>
        `;
    }

    #renderResource(resource: UaiContextResourceModel) {
        const content = this.#getResourceContent(resource);

        return html`
            <uui-box class="resource-item">
                <div class="resource-header">
                    <uui-input
                        label="Resource Name"
                        .value=${resource.name}
                        @change=${(e: Event) =>
                            this.#onResourceNameChange(resource.id, (e.target as HTMLInputElement).value)}
                        placeholder="Enter resource name"
                    ></uui-input>

                    <uui-select
                        label="Injection Mode"
                        .value=${resource.injectionMode}
                        @change=${(e: Event) =>
                            this.#onResourceInjectionModeChange(
                                resource.id,
                                (e.target as HTMLSelectElement).value as "Always" | "OnDemand"
                            )}
                    >
                        <uui-select-option value="Always">Always</uui-select-option>
                        <uui-select-option value="OnDemand">On Demand</uui-select-option>
                    </uui-select>

                    <uui-button
                        label="Remove"
                        look="secondary"
                        color="danger"
                        @click=${() => this.#onRemoveResource(resource.id)}
                    >
                        <uui-icon name="icon-trash"></uui-icon>
                    </uui-button>
                </div>

                <umb-input-markdown-editor
                    .value=${content}
                    @change=${(e: CustomEvent) =>
                        this.#onResourceDataChange(resource.id, e.detail.value ?? "")}
                    label="Content"
                ></umb-input-markdown-editor>
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

            #resources-box {
                margin-bottom: var(--uui-size-layout-1);
            }

            .empty-state {
                text-align: center;
                padding: var(--uui-size-space-5);
                color: var(--uui-color-text-alt);
            }

            .resource-list {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .resource-item {
                background: var(--uui-color-surface);
            }

            .resource-header {
                display: flex;
                gap: var(--uui-size-space-3);
                align-items: center;
                margin-bottom: var(--uui-size-space-3);
            }

            .resource-header uui-input {
                flex: 1;
            }

            .resource-header uui-select {
                width: 150px;
            }

            umb-input-markdown-editor {
                --umb-input-markdown-editor-height: 200px;
            }
        `,
    ];
}

export default UaiContextDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-context-details-workspace-view": UaiContextDetailsWorkspaceViewElement;
    }
}
