import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbBindToValidation } from "@umbraco-cms/backoffice/validation";
import type { UUISelectEvent } from "@umbraco-cms/backoffice/external/uui";
import type { UaiProfileDetailModel, UaiModelRef, UaiChatProfileSettings, UaiEmbeddingProfileSettings, UaiSpeechToTextProfileSettings, UaiImageGenerationProfileSettings } from "../../../types.js";
import { isChatSettings, isEmbeddingSettings, isSpeechToTextSettings, isImageGenerationSettings } from "../../../types.js";
import {
    UaiPartialUpdateCommand,
    isCapabilitySettingSupported,
    isProfileSettingSupported,
    getSupportedImageSizes,
} from "../../../../core/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";
import type { UaiConnectionItemModel, UaiModelDescriptorModel } from "../../../../connection/types.js";
import { UaiConnectionCapabilityRepository, UaiConnectionModelsRepository } from "../../../../connection/repository";
import { UaiProviderDetailRepository } from "../../../../provider/repository/detail/provider-detail.repository.js";
import type { UaiProviderDetailModel } from "../../../../provider/types.js";
import type { UaiEditableModelSchemaModel } from "../../../../core/types.js";
import type { UaiModelEditorChangeEventDetail } from "../../../../core/components/exports.js";

/**
 * Field key of the core temperature setting, as providers declare it in the model metadata.
 */
const UAI_TEMPERATURE_FIELD_KEY = "temperature";

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

    #observedTemperatureEditor?: Element;
    #observedTemperatureEditorWidth = 0;

    /**
     * Keeps the temperature slider's step markers aligned with its track.
     *
     * `uui-slider` measures the track once, when it first renders, and thereafter only when the window
     * resizes — so any later width change leaves the markers spaced for the old width, running past the
     * end of the track. A scrollbar appearing as the editor fills out is enough to trigger it. Re-firing
     * the event the slider already listens for is the supported way to make it measure again.
     *
     * The container is observed rather than the slider itself: `umb-input-slider` declares no display on
     * its host, so it is an inline element, and a ResizeObserver reports nothing for those.
     */
    #temperatureResizeObserver = new ResizeObserver((entries) => {
        const width = Math.round(entries[0]?.contentRect.width ?? 0);
        if (width === 0 || width === this.#observedTemperatureEditorWidth) return;

        this.#observedTemperatureEditorWidth = width;
        window.dispatchEvent(new Event("resize"));
    });

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

    override disconnectedCallback() {
        this.#temperatureResizeObserver.disconnect();
        super.disconnectedCallback();
    }

    protected override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);

        const editor = this.shadowRoot?.querySelector(".temperature-editor") ?? undefined;
        if (editor === this.#observedTemperatureEditor) return;

        if (this.#observedTemperatureEditor) {
            this.#temperatureResizeObserver.unobserve(this.#observedTemperatureEditor);
        }

        this.#observedTemperatureEditor = editor;
        this.#observedTemperatureEditorWidth = 0;
        if (editor) this.#temperatureResizeObserver.observe(editor);
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
     * Returns the stored core profile settings with anything the given model cannot accept cleared, or
     * `undefined` when nothing needs to change (which the partial update command skips).
     *
     * Same reasoning as {@link #pruneCapabilitySettings}: a value the model rejects would otherwise sit in
     * the profile with no field showing it, and be dropped or rejected on every request.
     */
    #pruneProfileSettings(modelId: string): UaiChatProfileSettings | UaiImageGenerationProfileSettings | undefined {
        const metadata = this.#getModelMetadata(modelId);

        const chatSettings = this.#getChatSettings();
        if (chatSettings?.temperature !== null && chatSettings?.temperature !== undefined) {
            return isProfileSettingSupported(metadata, UAI_TEMPERATURE_FIELD_KEY)
                ? undefined
                : { ...chatSettings, temperature: null };
        }

        const imageSettings = this.#getImageGenerationSettings();
        if (imageSettings?.size) {
            // Only prune against a model that actually declares its sizes. An empty list is silence, and
            // clearing a deliberate size because a provider described nothing would be worse than keeping it.
            const sizes = getSupportedImageSizes(metadata);
            return sizes.length === 0 || sizes.includes(imageSettings.size)
                ? undefined
                : { ...imageSettings, size: null };
        }

        return undefined;
    }

    /**
     * Gets the metadata for a model from the loaded model list, which carries the provider's per-model
     * settings declarations alongside the display name.
     */
    #getModelMetadata(modelId: string | undefined): Record<string, string> | undefined {
        if (!modelId) return undefined;
        return this._availableModels.find((m) => m.model.modelId === modelId)?.metadata;
    }

    #onTemperatureChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const temperature = target.value ? parseFloat(target.value) : null;
        this.#updateChatSettings({ temperature });
    }

    /**
     * Returns temperature to unset, so the provider's own default applies again.
     */
    #onTemperatureClear(event: Event) {
        event.stopPropagation();
        this.#updateChatSettings({ temperature: null });
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
                guardrailIds: updates.guardrailIds ?? [],
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: chatSettings }, "settings"),
        );
    }

    #onDimensionsChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const value = target.value;
        const dimensions = value ? parseInt(value, 10) : null;
        this.#updateEmbeddingSettings({ dimensions });
    }

    #updateEmbeddingSettings(updates: Partial<UaiEmbeddingProfileSettings>) {
        const currentSettings = this._model?.settings ?? null;
        const embeddingSettings: UaiEmbeddingProfileSettings = isEmbeddingSettings(currentSettings)
            ? { ...currentSettings, ...updates }
            : {
                $type: "embedding",
                dimensions: updates.dimensions ?? null,
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: embeddingSettings }, "settings"),
        );
    }

    /**
     * Gets the current chat settings, or null if not a chat profile.
     */
    #getChatSettings(): UaiChatProfileSettings | null {
        return isChatSettings(this._model?.settings ?? null) ? (this._model!.settings as UaiChatProfileSettings) : null;
    }

    #getEmbeddingSettings(): UaiEmbeddingProfileSettings | null {
        return isEmbeddingSettings(this._model?.settings ?? null) ? (this._model!.settings as UaiEmbeddingProfileSettings) : null;
    }

    #onLanguageChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        const language = target.value || null;
        this.#updateSpeechToTextSettings({ language });
    }

    #updateSpeechToTextSettings(updates: Partial<UaiSpeechToTextProfileSettings>) {
        const currentSettings = this._model?.settings ?? null;
        const sttSettings: UaiSpeechToTextProfileSettings = isSpeechToTextSettings(currentSettings)
            ? { ...currentSettings, ...updates }
            : {
                $type: "speechToText",
                language: updates.language ?? null,
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: sttSettings }, "settings"),
        );
    }

    #getSpeechToTextSettings(): UaiSpeechToTextProfileSettings | null {
        return isSpeechToTextSettings(this._model?.settings ?? null) ? (this._model!.settings as UaiSpeechToTextProfileSettings) : null;
    }

    #onImageSizeChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#updateImageGenerationSettings({ size: target.value || null });
    }

    /**
     * Renders the image size as a dropdown of the sizes the selected model declares, falling back to free
     * text when it declares none.
     *
     * The sizes have been travelling on the model list since image generation shipped, with nothing reading
     * them, so a size the model rejects saved cleanly and failed at generation time. The fallback matters as
     * much as the dropdown: declarations are negative, so a model a provider says nothing about must stay
     * typeable rather than being restricted to an empty list.
     */
    #renderImageSize(imageSettings: UaiImageGenerationProfileSettings | null) {
        const sizes = getSupportedImageSizes(this.#getModelMetadata(this._model?.model?.modelId));
        const size = imageSettings?.size ?? "";

        return html`
            <umb-property-layout
                label="Size"
                description=${sizes.length > 0
                    ? "Default image size. Leave empty for the provider default."
                    : 'Default image size as "{width}x{height}" (e.g. "1024x1024"). Leave empty for the provider default.'}
            >
                ${sizes.length > 0
                    ? html`
                        <uui-select
                            slot="editor"
                            .options=${this.#getImageSizeOptions(sizes, size)}
                            @change=${this.#onImageSizeChange}
                        ></uui-select>
                    `
                    : html`
                        <uui-input
                            slot="editor"
                            type="text"
                            .value=${size}
                            @input=${this.#onImageSizeChange}
                            placeholder="Provider default"
                        ></uui-input>
                    `}
            </umb-property-layout>
        `;
    }

    /**
     * Builds the size options, cached against the inputs that decide them.
     *
     * A fresh array on every render pushes a new config into the CMS dropdown, which rebuilds its derived
     * state and loses the empty "provider default" entry — the same trap the capability-settings schema
     * works around.
     */
    #getImageSizeOptions(sizes: string[], selected: string): Array<{ name: string; value: string; selected?: boolean }> {
        const cacheKey = `${sizes.join(",")}|${selected}`;
        if (this.#cachedImageSizeOptionsKey === cacheKey) return this.#cachedImageSizeOptions!;

        // A model that declares sizes can still be left unset, so the profile falls back to the provider's
        // own default — the same three-state thinking as temperature.
        const options = [
            { name: "Provider default", value: "", selected: selected === "" },
            ...sizes.map((s) => ({ name: s, value: s, selected: s === selected })),
        ];

        // A stored size the model doesn't list would otherwise vanish from the dropdown and read as unset.
        // Pruning on model change handles the normal path; this covers a value saved before a declaration
        // existed, or one a provider has since dropped.
        if (selected !== "" && !sizes.includes(selected)) {
            options.push({ name: `${selected} (not listed for this model)`, value: selected, selected: true });
        }

        this.#cachedImageSizeOptionsKey = cacheKey;
        this.#cachedImageSizeOptions = options;

        return options;
    }

    #cachedImageSizeOptionsKey?: string;
    #cachedImageSizeOptions?: Array<{ name: string; value: string; selected?: boolean }>;

    #onImageQualityChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#updateImageGenerationSettings({ quality: target.value || null });
    }

    #onImageStyleChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#updateImageGenerationSettings({ style: target.value || null });
    }

    #onImageMediaTypeChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLInputElement;
        this.#updateImageGenerationSettings({ mediaType: target.value || null });
    }

    #updateImageGenerationSettings(updates: Partial<UaiImageGenerationProfileSettings>) {
        const currentSettings = this._model?.settings ?? null;
        const imageSettings: UaiImageGenerationProfileSettings = isImageGenerationSettings(currentSettings)
            ? { ...currentSettings, ...updates }
            : {
                $type: "imageGeneration",
                size: updates.size ?? null,
                quality: updates.quality ?? null,
                style: updates.style ?? null,
                mediaType: updates.mediaType ?? null,
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: imageSettings }, "settings"),
        );
    }

    #getImageGenerationSettings(): UaiImageGenerationProfileSettings | null {
        return isImageGenerationSettings(this._model?.settings ?? null) ? (this._model!.settings as UaiImageGenerationProfileSettings) : null;
    }

    /**
     * Renders capability-specific settings based on the profile's capability.
     */
    #renderProfileSettings() {
        if (!this._model) return nothing;

        const capability = this._model.capability.toLowerCase();

        if (capability === "chat") {
            return this.#renderChatSettings();
        }

        if (capability === "embedding") {
            return this.#renderEmbeddingSettings();
        }

        if (capability === "speechtotext") {
            return this.#renderSpeechToTextSettings();
        }

        if (capability === "imagegeneration") {
            return this.#renderImageGenerationSettings();
        }

        return nothing;
    }

    /**
     * Renders chat-specific settings (temperature, max tokens, system prompt).
     */
    #renderChatSettings() {
        const chatSettings = this.#getChatSettings();

        return html`
            <uui-box headline="System Settings">
                ${this.#renderTemperature(chatSettings)}

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

    /**
     * Renders the temperature control, in the same two states the provider-declared settings use: absent
     * when the selected model rejects the setting, otherwise editable.
     *
     * An editable slider cannot express "unset" on its own — with no value it parks at its minimum, which
     * reads as a deliberate 0 — so an unset slider is dimmed, and the clear button beside it is how the
     * value gets given back. Nothing is stored until the slider is moved, so an untouched profile keeps its
     * null. Any value already stored is cleared when a model that rejects it is selected, so hiding the
     * field never hides a value that is still being sent.
     */
    #renderTemperature(chatSettings: UaiChatProfileSettings | null) {
        const metadata = this.#getModelMetadata(this._model?.model?.modelId);
        if (!isProfileSettingSupported(metadata, UAI_TEMPERATURE_FIELD_KEY)) {
            return nothing;
        }

        const temperature = chatSettings?.temperature ?? null;

        return html`
            <umb-property-layout
                label="Temperature"
                description="Controls randomness (0.0 = deterministic, 2.0 = very random). Clear it to use the provider's default."
            >
                <div slot="editor" class="temperature-editor">
                    <umb-input-slider
                        class=${temperature === null ? "unset" : ""}
                        label="Temperature"
                        .valueLow=${temperature ?? undefined}
                        .min=${0}
                        .max=${2}
                        .step=${0.1}
                        @change=${this.#onTemperatureChange}
                    ></umb-input-slider>
                    <uui-button
                        compact
                        look="secondary"
                        label="Clear temperature"
                        title="Clear temperature"
                        @click=${this.#onTemperatureClear}
                    >
                        <uui-icon name="icon-trash"></uui-icon>
                    </uui-button>
                </div>
            </umb-property-layout>
        `;
    }

    #renderEmbeddingSettings() {
        const embeddingSettings = this.#getEmbeddingSettings();

        return html`
            <uui-box headline="System Settings">
                <umb-property-layout
                    label="Dimensions"
                    description="Number of dimensions for generated embeddings. Leave empty to use the model's default."
                >
                    <uui-input
                        slot="editor"
                        type="number"
                        min="1"
                        max="1998"
                        .value=${embeddingSettings?.dimensions?.toString() ?? ""}
                        @input=${this.#onDimensionsChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>
            </uui-box>
        `;
    }

    #renderSpeechToTextSettings() {
        const sttSettings = this.#getSpeechToTextSettings();

        return html`
            <uui-box headline="System Settings">
                <umb-property-layout
                    label="Language"
                    description="BCP-47 language hint for transcription (e.g., &quot;en&quot;, &quot;de&quot;, &quot;ja&quot;). Leave empty for auto-detection."
                >
                    <uui-input
                        slot="editor"
                        type="text"
                        .value=${sttSettings?.language ?? ""}
                        @input=${this.#onLanguageChange}
                        placeholder="Auto-detect"
                    ></uui-input>
                </umb-property-layout>
            </uui-box>
        `;
    }

    #renderImageGenerationSettings() {
        const imageSettings = this.#getImageGenerationSettings();

        return html`
            <uui-box headline="System Settings">
                ${this.#renderImageSize(imageSettings)}

                <umb-property-layout label="Media Type" description="Output image encoding (e.g. &quot;image/png&quot;, &quot;image/jpeg&quot;, &quot;image/webp&quot;). Supported values vary by model.">
                    <uui-input
                        slot="editor"
                        type="text"
                        .value=${imageSettings?.mediaType ?? ""}
                        @input=${this.#onImageMediaTypeChange}
                        placeholder="Provider default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Quality" description="Provider-specific quality hint (e.g. &quot;hd&quot; for DALL·E 3, &quot;high&quot; for gpt-image-1). Values vary by model.">
                    <uui-input
                        slot="editor"
                        type="text"
                        .value=${imageSettings?.quality ?? ""}
                        @input=${this.#onImageQualityChange}
                        placeholder="Provider default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Style" description="Provider-specific style hint (e.g. &quot;vivid&quot;, &quot;natural&quot; for DALL·E 3). Values vary by model.">
                    <uui-input
                        slot="editor"
                        type="text"
                        .value=${imageSettings?.style ?? ""}
                        @input=${this.#onImageStyleChange}
                        placeholder="Provider default"
                    ></uui-input>
                </umb-property-layout>
            </uui-box>
        `;
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
