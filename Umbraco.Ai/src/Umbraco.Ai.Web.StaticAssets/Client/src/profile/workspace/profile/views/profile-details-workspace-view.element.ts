import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import type { UaiProfileDetailModel, UaiModelRef, UaiChatProfileSettings } from "../../../types.js";
import { isChatSettings } from "../../../types.js";
import { UAI_EMPTY_GUID, UaiPartialUpdateCommand, formatDateTime } from "../../../../core/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";
import type { UaiConnectionItemModel, UaiModelDescriptorModel } from "../../../../connection/types.js";
import { UaiConnectionCapabilityRepository, UaiConnectionModelsRepository } from "../../../../connection/repository";
import "../../../../core/version-history/components/version-history-table/version-history-table.element.js";

/**
 * Workspace view for Profile details.
 * Displays capability (read-only), connection selection, model selection, and advanced settings.
 */
@customElement("uai-profile-details-workspace-view")
export class UaiProfileDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;
    #connectionRepository = new UaiConnectionCapabilityRepository(this);
    #connectionModelsRepository = new UaiConnectionModelsRepository(this);
    #localize = new UmbLocalizationController(this);

    @state()
    private _model?: UaiProfileDetailModel;

    @state()
    private _connections: UaiConnectionItemModel[] = [];

    @state()
    private _availableModels: UaiModelDescriptorModel[] = [];

    @state()
    private _loadingModels = false;

    constructor() {
        super();
        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    const previousCapability = this._model?.capability;
                    const previousConnectionId = this._model?.connectionId;
                    this._model = model;

                    // Reload connections if capability changed (or first load)
                    if (model?.capability && model.capability !== previousCapability) {
                        this.#loadConnectionsAndModels(model.connectionId, model.capability);
                    } else if (model?.connectionId && model.connectionId !== previousConnectionId) {
                        // Only load models if connection changed (and connections already loaded)
                        this.#loadModelsForConnection(model.connectionId, model.capability);
                    }
                });
            }
        });
    }

    /**
     * Loads connections for the current capability, then loads models if a connection is selected.
     */
    async #loadConnectionsAndModels(connectionId: string | undefined, capability: string) {
        const { data } = await this.#connectionRepository.requestConnectionsByCapability(capability);
        if (data) {
            this._connections = data;

            // If a connection is already selected, load its models
            if (connectionId) {
                await this.#loadModelsForConnection(connectionId, capability);
            }
        }
    }

    async #loadModelsForConnection(connectionId: string, capability: string) {
        this._loadingModels = true;

        const { data, error } = await this.#connectionModelsRepository.requestModels({
            connectionId,
            capability,
        });

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
        const temperature = target.value ? parseFloat(target.value) : null;
        this.#updateChatSettings({ temperature });
    }

    #onMaxTokensChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const value = target.value;
        const maxTokens = value ? parseInt(value, 10) : null;
        this.#updateChatSettings({ maxTokens });
    }

    #onSystemPromptChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLTextAreaElement).value;
        const systemPromptTemplate = value || null;
        this.#updateChatSettings({ systemPromptTemplate });
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#updateChatSettings({ contextIds: picker.value });
    }

    /**
     * Updates chat-specific settings while preserving other settings values.
     */
    #updateChatSettings(updates: Partial<UaiChatProfileSettings>) {
        const currentSettings = this._model?.settings ?? null;
        const chatSettings: UaiChatProfileSettings = isChatSettings(currentSettings)
            ? { ...currentSettings, ...updates }
            : {
                $type: "chat",
                temperature: updates.temperature ?? null,
                maxTokens: updates.maxTokens ?? null,
                systemPromptTemplate: updates.systemPromptTemplate ?? null,
                contextIds: updates.contextIds ?? [],
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: chatSettings }, "settings")
        );
    }

    /**
     * Gets the current chat settings, or null if not a chat profile.
     */
    #getChatSettings(): UaiChatProfileSettings | null {
        return isChatSettings(this._model?.settings ?? null) ? this._model!.settings as UaiChatProfileSettings : null;
    }

    #getCapabilityLabel(capability: string): string {
        return this.#localize.term(`uaiCapabilities_${capability.toLowerCase()}`);
    }

    /**
     * Renders capability-specific settings based on the profile's capability.
     */
    #renderCapabilitySettings() {
        if (!this._model) return nothing;

        const capability = this._model.capability.toLowerCase();

        if (capability === "chat") {
            return this.#renderChatSettings();
        }

        // Embedding profiles have no additional settings currently
        return nothing;
    }

    /**
     * Renders chat-specific settings (temperature, max tokens, system prompt).
     */
    #renderChatSettings() {
        const chatSettings = this.#getChatSettings();

        return html`
            <uui-box headline="Settings">
                <umb-property-layout label="Temperature" description="Controls randomness (0.0 = deterministic, 2.0 = very random)">
                    <umb-input-slider
                        slot="editor"
                        label="Temperature"
                        .valueLow=${chatSettings?.temperature ?? 1}
                        .min=${0}
                        .max=${2}
                        .step=${0.1}
                        @change=${this.#onTemperatureChange}
                    ></umb-input-slider>
                </umb-property-layout>

                <umb-property-layout label="Max Tokens" description="Maximum number of tokens to generate">
                    <uui-input
                        slot="editor"
                        type="number"
                        min="1"
                        .value=${chatSettings?.maxTokens?.toString() ?? ""}
                        @input=${this.#onMaxTokensChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="System Prompt" description="System prompt template for this profile">
                    <uui-textarea
                        slot="editor"
                        .value=${chatSettings?.systemPromptTemplate ?? ""}
                        @input=${this.#onSystemPromptChange}
                        placeholder="Enter system prompt template..."
                        rows="6"
                    ></uui-textarea>
                </umb-property-layout>

                <umb-property-layout label="Contexts" description="Predefined contexts to include in chat sessions">
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${chatSettings?.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>
            </uui-box>
        `;
    }

    #getCurrentModelValue(): string {
        if (!this._model?.model) return "";
        return `${this._model.model.providerId}|${this._model.model.modelId}`;
    }

    #getConnectionOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        const options: Array<{ name: string; value: string; selected?: boolean }> = [
            { name: "-- Select Connection --", value: "" },
        ];

        for (const conn of this._connections) {
            options.push({
                name: conn.name,
                value: conn.unique,
                selected: conn.unique === this._model?.connectionId,
            });
        }

        return options;
    }

    #getModelOptions(): Array<{ name: string; value: string; selected?: boolean }> {
        const options: Array<{ name: string; value: string; selected?: boolean }> = [
            { name: "-- Select Model --", value: "" },
        ];

        const currentValue = this.#getCurrentModelValue();

        for (const modelDesc of this._availableModels) {
            const value = `${modelDesc.model.providerId}|${modelDesc.model.modelId}`;
            options.push({
                name: modelDesc.name,
                value,
                selected: value === currentValue,
            });
        }

        return options;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uai-workspace-editor-layout>
                <div>${this.#renderLeftColumn()}</div>
                <div slot="aside">${this.#renderRightColumn()}</div>
            </uai-workspace-editor-layout>
        `;
    }

    #renderLeftColumn() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">

                <umb-property-layout label="Connection" description="Select the AI connection to use">
                    <uui-select
                        slot="editor"
                        .value=${this._model.connectionId}
                        .options=${this.#getConnectionOptions()}
                        @change=${this.#onConnectionChange}
                        placeholder="Select a connection"
                    ></uui-select>
                </umb-property-layout>

                <umb-property-layout label="Model" description="Select the AI model to use">
                    ${this._loadingModels
                        ? html`<uui-loader-bar slot="editor"></uui-loader-bar>`
                        : html`
                            <uui-select
                                slot="editor"
                                .value=${this.#getCurrentModelValue()}
                                .options=${this.#getModelOptions()}
                                @change=${this.#onModelChange}
                                placeholder="Select a model"
                                ?disabled=${!this._model.connectionId || this._availableModels.length === 0}
                            ></uui-select>
                        `}
                </umb-property-layout>
            </uui-box>

            ${this.#renderCapabilitySettings()}

            ${this._model.tags.length > 0 ? html`
                <uui-box headline="Tags">
                    <div class="tags-container">
                        ${this._model.tags.map((tag) => html`<uui-tag>${tag}</uui-tag>`)}
                    </div>
                </uui-box>
            ` : nothing}

            ${this._model.unique && this._model.unique !== UAI_EMPTY_GUID ? html`
                <uai-version-history-table></uai-version-history-table>
            ` : nothing}
        `;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`<uui-box headline="Info">
            <umb-property-layout label="Id"  orientation="vertical">
               <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
            ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
            : this._model.unique}</div>
            </umb-property-layout>
            ${this._model.dateCreated ? html`
                <umb-property-layout label="Date Created" orientation="vertical">
                    <div slot="editor">${formatDateTime(this._model.dateCreated)}</div>
                </umb-property-layout>
            ` : ''}
            ${this._model.dateModified ? html`
                <umb-property-layout label="Date Modified" orientation="vertical">
                    <div slot="editor">${formatDateTime(this._model.dateModified)}</div>
                </umb-property-layout>
            ` : ''}
            <umb-property-layout label="Capability"  orientation="vertical">
                <div slot="editor">
                    <uui-tag color="default" look="outline">${this.#getCapabilityLabel(this._model.capability)}</uui-tag>
                </div>
            </umb-property-layout>
        </uui-box>`; 
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

            uui-select {
                width: 100%;
            }

            uui-input,
            umb-input-slider {
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

export default UaiProfileDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-details-workspace-view": UaiProfileDetailsWorkspaceViewElement;
    }
}
