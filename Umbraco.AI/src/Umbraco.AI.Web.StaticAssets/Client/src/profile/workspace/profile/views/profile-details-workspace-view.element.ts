import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { umbBindToValidation } from "@umbraco-cms/backoffice/validation";
import type { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import type { UaiProfileDetailModel, UaiModelRef, UaiProfileSettings, UaiImageGenerationProfileSettings } from "../../../types.js";
import { isChatSettings, isEmbeddingSettings, isSpeechToTextSettings, isImageGenerationSettings } from "../../../types.js";
import { UaiPartialUpdateCommand, isCapabilitySettingSupported, getSupportedImageSizes } from "../../../../core/index.js";
// Imported for the custom-element registrations as well as the rules: these live in this view's own chunk
// rather than the global barrel, so they load with the view that uses them.
import {
    UAI_CHAT_SETTING_RULES,
    UAI_EMBEDDING_SETTING_RULES,
    UAI_IMAGE_GENERATION_SETTING_RULES,
    UAI_SPEECH_TO_TEXT_SETTING_RULES,
    pruneDeclaredSettings,
} from "./settings/index.js";
import type { UaiProfileSettingsChangeEventDetail } from "./settings/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";
import type { UaiConnectionItemModel, UaiModelDescriptorModel } from "../../../../connection/types.js";
import { UaiConnectionCapabilityRepository, UaiConnectionModelsRepository } from "../../../../connection/repository";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";
import type { UaiEditableModelSchemaModel } from "../../../../core/types.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";

/**
 * Workspace view for Profile details.
 * Displays capability (read-only), connection selection, model selection, and advanced settings.
 */
@customElement("uai-profile-details-workspace-view")
export class UaiProfileDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;
    #connectionRepository = new UaiConnectionCapabilityRepository(this);
    #connectionModelsRepository = new UaiConnectionModelsRepository(this);
    #providerDetailRepository = new UaiProviderDetailRepository(this);

    @state()
    private _model?: UaiProfileDetailModel;

    @state()
    private _provider?: UaiProviderDetailModel;

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

            // If a connection is already selected, load its models and the provider's capability-settings schema
            if (connectionId) {
                await this.#loadModelsForConnection(connectionId, capability);
                await this.#loadProviderDetail(connectionId);
            }
        }
    }

    /**
     * Loads the provider detail (for its capability-settings schema) for the selected connection's provider.
     */
    async #loadProviderDetail(connectionId: string | undefined) {
        const providerId = this._connections.find((c) => c.unique === connectionId)?.providerId;
        if (!providerId) {
            this._provider = undefined;
            return;
        }

        const { data } = await this.#providerDetailRepository.requestById(providerId);
        this._provider = data;
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
        // Changing connection can change the provider, so clear any previously entered
        // provider-specific settings to avoid carrying an incompatible bag across providers.
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>(
                { connectionId, model: null, capabilitySettings: null },
                "connectionId",
            ),
        );
        // Load models and provider capability-settings schema for the new connection
        if (connectionId && this._model?.capability) {
            this.#loadModelsForConnection(connectionId, this._model.capability);
            this.#loadProviderDetail(connectionId);
        } else {
            this._availableModels = [];
            this._provider = undefined;
        }
    }

    #onModelChange(event: UUISelectEvent) {
        event.stopPropagation();
        const value = event.target.value as string;
        if (!value) {
            this.#workspaceContext?.handleCommand(
                new UaiPartialUpdateCommand<UaiProfileDetailModel>({ model: null }, "model"),
            );
            return;
        }

        const [providerId, modelId] = value.split("|");
        const model: UaiModelRef = { providerId, modelId };
        // Drop any stored provider settings the newly selected model doesn't accept. Without this they
        // stay persisted but invisible in the editor, and still get sent on every request.
        const capabilitySettings = this.#pruneCapabilitySettings(modelId);
        const settings = this.#pruneProfileSettings(modelId);
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ model, capabilitySettings, settings }, "model"),
        );
    }

    /**
     * Returns the stored capability settings with the entries the given model declares unsupported
     * removed, or `undefined` when nothing needs to change (which the partial update command skips).
     */
    #pruneCapabilitySettings(modelId: string): Record<string, unknown> | null | undefined {
        const current = this._model?.capabilitySettings;
        if (!current) return undefined;

        const metadata = this.#getModelMetadata(modelId);
        const entries = Object.entries(current).filter(([key]) => isCapabilitySettingSupported(metadata, key));

        if (entries.length === Object.keys(current).length) return undefined;

        return entries.length > 0 ? Object.fromEntries(entries) : null;
    }

    /**
     * Returns the stored core profile settings with anything the given model declares unsupported cleared,
     * or `undefined` when nothing needs to change (which the partial update command skips).
     *
     * Reads the same rule lists the settings elements render from, so a field that hides and a value that
     * clears cannot get out of step — they did once, and two settings were left with only the first.
     */
    #pruneProfileSettings(modelId: string): UaiProfileSettings | undefined {
        const metadata = this.#getModelMetadata(modelId);
        const settings = this._model?.settings ?? null;

        if (isChatSettings(settings)) {
            return pruneDeclaredSettings(settings, metadata, UAI_CHAT_SETTING_RULES);
        }

        if (isEmbeddingSettings(settings)) {
            return pruneDeclaredSettings(settings, metadata, UAI_EMBEDDING_SETTING_RULES);
        }

        if (isSpeechToTextSettings(settings)) {
            return pruneDeclaredSettings(settings, metadata, UAI_SPEECH_TO_TEXT_SETTING_RULES);
        }

        if (isImageGenerationSettings(settings)) {
            // Size is absent from the rules on purpose: its support is described by enumerating what a model
            // accepts, so a stored size is checked against that list instead.
            const pruned = pruneDeclaredSettings(settings, metadata, UAI_IMAGE_GENERATION_SETTING_RULES);
            return this.#pruneImageSize(pruned ?? settings, metadata) ?? pruned;
        }

        return undefined;
    }

    /**
     * Clears a stored image size the given model does not list. An empty list is silence, not a refusal, so
     * a deliberate size survives a model that describes nothing.
     */
    #pruneImageSize(
        settings: UaiImageGenerationProfileSettings,
        metadata: Record<string, string> | undefined,
    ): UaiImageGenerationProfileSettings | undefined {
        if (!settings.size) return undefined;

        const sizes = getSupportedImageSizes(metadata);
        if (sizes.length === 0 || sizes.includes(settings.size)) return undefined;

        return { ...settings, size: null };
    }

    /**
     * Gets the metadata for a model from the loaded model list, which carries the provider's per-model
     * settings declarations alongside the display name.
     */
    #getModelMetadata(modelId: string | undefined): Record<string, string> | undefined {
        if (!modelId) return undefined;
        return this._availableModels.find((m) => m.model.modelId === modelId)?.metadata;
    }





















    /**
     * Renders capability-specific settings based on the profile's capability.
     */
    /**
     * Renders the capability's own settings element, handing it the stored settings and the selected model's
     * declarations. Each element owns its fields, including which of them a declaration hides.
     */
    #renderProfileSettings() {
        if (!this._model) return nothing;

        const metadata = this.#getModelMetadata(this._model.model?.modelId);
        const settings = this._model.settings ?? null;

        switch (this._model.capability.toLowerCase()) {
            case "chat":
                return html`
                    <uai-chat-profile-settings
                        .settings=${isChatSettings(settings) ? settings : null}
                        .metadata=${metadata}
                        @uai-profile-settings-change=${this.#onProfileSettingsChange}
                    ></uai-chat-profile-settings>
                `;
            case "embedding":
                return html`
                    <uai-embedding-profile-settings
                        .settings=${isEmbeddingSettings(settings) ? settings : null}
                        .metadata=${metadata}
                        @uai-profile-settings-change=${this.#onProfileSettingsChange}
                    ></uai-embedding-profile-settings>
                `;
            case "speechtotext":
                return html`
                    <uai-speech-to-text-profile-settings
                        .settings=${isSpeechToTextSettings(settings) ? settings : null}
                        .metadata=${metadata}
                        @uai-profile-settings-change=${this.#onProfileSettingsChange}
                    ></uai-speech-to-text-profile-settings>
                `;
            case "imagegeneration":
                return html`
                    <uai-image-generation-profile-settings
                        .settings=${isImageGenerationSettings(settings) ? settings : null}
                        .metadata=${metadata}
                        @uai-profile-settings-change=${this.#onProfileSettingsChange}
                    ></uai-image-generation-profile-settings>
                `;
            default:
                return nothing;
        }
    }

    /**
     * Stores whatever the capability's settings element hands back. The element sends its settings complete,
     * so this does not need to know which field moved.
     */
    #onProfileSettingsChange(event: CustomEvent<UaiProfileSettingsChangeEventDetail>) {
        event.stopPropagation();

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: event.detail.settings }, "settings"),
        );
    }







    /**
     * Gets the provider-declared capability-settings schema for the current capability, if any.
     * Keyed by capability name (e.g. "Chat"); matched case-insensitively.
     *
     * Fields the selected model declares unsupported are filtered out — support for these settings
     * varies by model (reasoning effort is an o-series/GPT-5 knob; a thinking budget is rejected by the
     * newest Claude models), and the declarations ride along on the already-loaded model list.
     */
    #getCapabilitySettingsSchema(): UaiEditableModelSchemaModel | undefined {
        const capability = this._model?.capability;
        const schemas = this._provider?.capabilitySettingsSchemas;
        if (!capability || !schemas) return undefined;

        const modelId = this._model?.model?.modelId;

        // Filtering produces a new schema object, so the result is cached against the inputs that decide it.
        // A fresh object on every render would push a new config into each property editor, and some rebuild
        // derived state when that happens — the CMS dropdown loses its empty "clear the value" option.
        const cacheKey = `${capability}|${modelId ?? ""}|${schemas === this.#cachedSchemas ? "same" : "new"}`;
        if (this.#cachedSchemaKey === cacheKey) return this.#cachedSchema;

        const key = Object.keys(schemas).find((k) => k.toLowerCase() === capability.toLowerCase());
        const schema = key ? schemas[key] : undefined;

        let result: UaiEditableModelSchemaModel | undefined;
        if (schema) {
            const metadata = this.#getModelMetadata(modelId);
            const fields = schema.fields.filter((field) => isCapabilitySettingSupported(metadata, field.key));
            result = fields.length === schema.fields.length ? schema : { ...schema, fields };
        }

        this.#cachedSchemas = schemas;
        this.#cachedSchemaKey = cacheKey;
        this.#cachedSchema = result;

        return result;
    }

    #cachedSchemas?: Record<string, UaiEditableModelSchemaModel>;
    #cachedSchemaKey?: string;
    #cachedSchema?: UaiEditableModelSchemaModel;

    #onCapabilitySettingsChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        e.stopPropagation();
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ capabilitySettings: e.detail.model }, "capabilitySettings"),
        );
    }

    /**
     * Renders the provider-declared, profile-level settings (e.g. reasoning effort) using the
     * shared schema-driven model editor. Renders nothing when the provider declares no extras.
     *
     * Also renders nothing until a model is selected: which of these settings apply is a per-model fact,
     * so with no model chosen there is nothing to filter against and every field would be offered —
     * including ones the model about to be picked rejects.
     */
    #renderCapabilitySettings() {
        if (!this._model?.model?.modelId) return nothing;

        const schema = this.#getCapabilitySettingsSchema();
        if (!schema || schema.fields.length === 0) return nothing;

        return html`
            <uai-model-editor
                .schema=${schema}
                .model=${this._model?.capabilitySettings ?? undefined}
                empty-message="This provider has no additional settings."
                default-group="Provider settings"
                @change=${this.#onCapabilitySettingsChange}
            >
            </uai-model-editor>
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
            <uui-box headline="General">
                <umb-property-layout label="Connection" description="Select the AI connection to use" mandatory>
                    <uui-select
                        slot="editor"
                        name="connectionId"
                        .value=${this._model.connectionId}
                        .options=${this.#getConnectionOptions()}
                        @change=${this.#onConnectionChange}
                        placeholder="Select a connection"
                        required
                        ${umbBindToValidation(this, "$.connectionId", this._model.connectionId)}
                    ></uui-select>
                </umb-property-layout>

                <umb-property-layout label="Model" description="Select the AI model to use" mandatory>
                    <div slot="editor">
                        ${this._loadingModels ? html`<uui-loader-bar></uui-loader-bar>` : nothing}
                        <uui-select
                            name="model"
                            .value=${this.#getCurrentModelValue()}
                            .options=${this.#getModelOptions()}
                            @change=${this.#onModelChange}
                            placeholder="Select a model"
                            ?disabled=${!this._model.connectionId || this._availableModels.length === 0}
                            required
                            ${umbBindToValidation(this, "$.model", this._model.model)}
                            class="${this._loadingModels ? "hidden" : ""}"
                        ></uui-select>
                    </div>
                </umb-property-layout>
            </uui-box>

            ${this.#renderProfileSettings()}
            ${this.#renderCapabilitySettings()}
            ${this._model.tags.length > 0
                ? html`
                      <uui-box headline="Tags">
                          <div class="tags-container">
                              ${this._model.tags.map((tag) => html`<uui-tag>${tag}</uui-tag>`)}
                          </div>
                      </uui-box>
                  `
                : nothing}
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
            uui-box:not(:first-child),
            uai-model-editor:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uui-select {
                width: 100%;
            }

            uui-input,
            umb-input-slider {
                width: 100%;
            }

            /* The clear button is taken out of flow with room reserved for it, rather than laid out as a
               flex sibling: the slider measures its own width to decide whether the step markers fit, it
               does that once before a flex row has settled, and only ever recomputes on a window resize —
               so a slider sized by flex loses its markers. A plain full-width slider measures correctly. */
            .temperature-editor {
                position: relative;
                padding-right: calc(30px + var(--uui-size-space-2));
            }
            .temperature-editor uui-button {
                position: absolute;
                right: 0;
                /* Centred on the track, which sits at the top of the slider's box above the row it
                   reserves for step labels — not on the box itself. */
                top: 9px;
                transform: translateY(-50%);
            }
            /* Dimmed while no value is stored, so the slider's resting position at its minimum doesn't
               read as the profile's temperature. */
            .temperature-editor umb-input-slider.unset {
                opacity: 0.5;
            }

            uui-textarea {
                width: 100%;
            }

            uui-tag {
                white-space: nowrap;
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

            .hidden {
                display: none;
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
