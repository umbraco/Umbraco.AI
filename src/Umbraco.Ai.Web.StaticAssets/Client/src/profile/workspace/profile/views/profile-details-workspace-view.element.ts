import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import type { UaiProfileDetailModel, UaiModelRef } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";
import { UaiConnectionCollectionRepository } from "../../../../connection/repository/collection/connection-collection.repository.js";
import { UaiProviderItemRepository } from "../../../../provider/repository/item/provider-item.repository.js";
import type { UaiConnectionItemModel } from "../../../../connection/types.js";
import type { UaiProviderItemModel } from "../../../../provider/types.js";
import { ProvidersService } from "../../../../api/sdk.gen.js";
import type { ModelDescriptorResponseModel } from "../../../../api/types.gen.js";

/**
 * Workspace view for Profile details.
 * Displays capability (read-only), connection selection, model selection, and advanced settings.
 */
@customElement("uai-profile-details-workspace-view")
export class UaiProfileDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;
    #connectionRepository = new UaiConnectionCollectionRepository(this);
    #providerRepository = new UaiProviderItemRepository(this);

    @state()
    private _model?: UaiProfileDetailModel;

    @state()
    private _connections: UaiConnectionItemModel[] = [];

    @state()
    private _filteredConnections: UaiConnectionItemModel[] = [];

    @state()
    private _providers: UaiProviderItemModel[] = [];

    @state()
    private _availableModels: ModelDescriptorResponseModel[] = [];

    @state()
    private _loadingModels = false;

    constructor() {
        super();
        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    const previousConnectionId = this._model?.connectionId;
                    this._model = model;
                    this.#filterConnectionsByCapability();
                    // Only load models if connection changed
                    if (model?.connectionId && model.connectionId !== previousConnectionId) {
                        this.#loadModelsForConnection(model.connectionId, model.capability);
                    }
                });
            }
        });
        this.#loadConnections();
        this.#loadProviders();
    }

    async #loadConnections() {
        const { data } = await this.#connectionRepository.requestCollection({ skip: 0, take: 100 });
        if (data) {
            this._connections = data.items;
            this.#filterConnectionsByCapability();
        }
    }

    async #loadProviders() {
        const { data } = await this.#providerRepository.requestItems();
        if (data) {
            this._providers = data;
            this.#filterConnectionsByCapability();
            // If we have a connection already selected, load its models
            if (this._model?.connectionId) {
                this.#loadModelsForConnection(this._model.connectionId, this._model.capability);
            }
        }
    }

    #filterConnectionsByCapability() {
        if (!this._model?.capability || this._connections.length === 0 || this._providers.length === 0) {
            this._filteredConnections = this._connections;
            return;
        }

        // Filter connections by those whose provider supports the required capability
        this._filteredConnections = this._connections.filter((conn) => {
            const provider = this._providers.find((p) => p.id === conn.providerId);
            return provider?.capabilities?.includes(this._model!.capability);
        });
    }

    async #loadModelsForConnection(connectionId: string, capability: string) {
        const connection = this._connections.find((c) => c.unique === connectionId);
        if (!connection) {
            this._availableModels = [];
            return;
        }

        this._loadingModels = true;

        const { data, error } = await tryExecute(
            this,
            ProvidersService.getProvidersByIdModels({
                path: { id: connection.providerId },
                query: { connectionId, capability },
            })
        );

        this._loadingModels = false;

        if (error || !data) {
            this._availableModels = [];
            return;
        }

        this._availableModels = data;
    }

    #onConnectionChange(event: UUISelectEvent) {
        event.stopPropagation();
        const connectionId = event.target.value as string;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ connectionId, model: null }, "connectionId")
        );
        // Load models for the new connection
        if (connectionId && this._model?.capability) {
            this.#loadModelsForConnection(connectionId, this._model.capability);
        } else {
            this._availableModels = [];
        }
    }

    #onModelChange(event: UUISelectEvent) {
        event.stopPropagation();
        const value = event.target.value as string;
        if (!value) {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiProfileDetailModel>({ model: null }, "model")
            );
            return;
        }

        const [providerId, modelId] = value.split("|");
        const model: UaiModelRef = { providerId, modelId };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ model }, "model")
        );
    }

    #onTemperatureChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const value = target.value;
        const temperature = value ? parseFloat(value) : null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ temperature }, "temperature")
        );
    }

    #onMaxTokensChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const value = target.value;
        const maxTokens = value ? parseInt(value, 10) : null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ maxTokens }, "maxTokens")
        );
    }

    #onSystemPromptChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLTextAreaElement).value;
        const systemPromptTemplate = value || null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ systemPromptTemplate }, "systemPromptTemplate")
        );
    }

    #getCapabilityLabel(capability: string): string {
        const labels: Record<string, string> = {
            chat: "Chat",
            embedding: "Embedding",
        };
        return labels[capability] ?? capability;
    }

    #getCurrentModelValue(): string {
        if (!this._model?.model) return "";
        return `${this._model.model.providerId}|${this._model.model.modelId}`;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="Profile Configuration">
                <umb-property-layout label="Capability" description="The AI capability this profile is configured for">
                    <div slot="editor" class="capability-display">
                        <uui-tag color="primary">${this.#getCapabilityLabel(this._model.capability)}</uui-tag>
                    </div>
                </umb-property-layout>

                <umb-property-layout label="Connection" description="Select the AI connection to use">
                    <uui-select
                        slot="editor"
                        .value=${this._model.connectionId}
                        @change=${this.#onConnectionChange}
                        placeholder="Select a connection"
                    >
                        <uui-select-option value="">-- Select Connection --</uui-select-option>
                        ${this._filteredConnections.map(
                            (conn) => html`
                                <uui-select-option value=${conn.unique}>${conn.name}</uui-select-option>
                            `
                        )}
                    </uui-select>
                </umb-property-layout>

                <umb-property-layout label="Model" description="Select the AI model to use">
                    ${this._loadingModels
                        ? html`<uui-loader-bar slot="editor"></uui-loader-bar>`
                        : html`
                            <uui-select
                                slot="editor"
                                .value=${this.#getCurrentModelValue()}
                                @change=${this.#onModelChange}
                                placeholder="Select a model"
                                ?disabled=${!this._model.connectionId || this._availableModels.length === 0}
                            >
                                <uui-select-option value="">-- Select Model --</uui-select-option>
                                ${this._availableModels.map(
                                    (modelDesc) => html`
                                        <uui-select-option value="${modelDesc.model.providerId}|${modelDesc.model.modelId}">
                                            ${modelDesc.name}
                                        </uui-select-option>
                                    `
                                )}
                            </uui-select>
                        `}
                </umb-property-layout>
            </uui-box>

            <uui-box headline="Advanced Settings">
                <umb-property-layout label="Temperature" description="Controls randomness (0.0 = deterministic, 2.0 = very random)">
                    <uui-input
                        slot="editor"
                        type="number"
                        min="0"
                        max="2"
                        step="0.1"
                        .value=${this._model.temperature?.toString() ?? ""}
                        @input=${this.#onTemperatureChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Max Tokens" description="Maximum number of tokens to generate">
                    <uui-input
                        slot="editor"
                        type="number"
                        min="1"
                        .value=${this._model.maxTokens?.toString() ?? ""}
                        @input=${this.#onMaxTokensChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="System Prompt" description="System prompt template for this profile">
                    <uui-textarea
                        slot="editor"
                        .value=${this._model.systemPromptTemplate ?? ""}
                        @input=${this.#onSystemPromptChange}
                        placeholder="Enter system prompt template..."
                        rows="6"
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

            .capability-display {
                display: flex;
                align-items: center;
            }

            uui-select {
                width: 100%;
            }

            uui-input {
                width: 100%;
            }

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

export default UaiProfileDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-details-workspace-view": UaiProfileDetailsWorkspaceViewElement;
    }
}
