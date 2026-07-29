import { css, html, customElement, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { getSupportedImageSizes } from "../../../../../core/index.js";
import type { UaiImageGenerationProfileSettings } from "../../../../types.js";
import { isRuleSupported } from "./declared-settings.js";
import { UAI_IMAGE_GENERATION_SETTING_RULES } from "./rules.js";
import { uaiProfileSettingsChangeEvent } from "./profile-settings-change.event.js";

/**
 * Image-generation profile settings: size and media type.
 */
@customElement("uai-image-generation-profile-settings")
export class UaiImageGenerationProfileSettingsElement extends UmbLitElement {
    @property({ type: Object })
    settings: UaiImageGenerationProfileSettings | null = null;

    /** The selected model descriptor's metadata, carrying the provider's per-model declarations. */
    @property({ type: Object })
    metadata?: Record<string, string>;

    #onSizeChange(event: Event) {
        event.stopPropagation();
        this.#update({ size: (event.target as HTMLInputElement).value || null });
    }

    #onMediaTypeChange(event: Event) {
        event.stopPropagation();
        this.#update({ mediaType: (event.target as HTMLInputElement).value || null });
    }

    #update(updates: Partial<UaiImageGenerationProfileSettings>) {
        this.dispatchEvent(uaiProfileSettingsChangeEvent<UaiImageGenerationProfileSettings>({
            $type: "imageGeneration",
            size: this.settings?.size ?? null,
            mediaType: this.settings?.mediaType ?? null,
            ...updates,
        }));
    }

    /**
     * Renders size as a dropdown of the sizes the selected model declares, falling back to free text when it
     * declares none.
     *
     * The fallback matters as much as the dropdown: declarations are negative, so a model a provider says
     * nothing about must stay typeable rather than be restricted to an empty list.
     */
    #renderSize() {
        const sizes = getSupportedImageSizes(this.metadata);
        const size = this.settings?.size ?? "";

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
                            .options=${this.#getSizeOptions(sizes, size)}
                            @change=${this.#onSizeChange}
                        ></uui-select>
                    `
                    : html`
                        <uui-input
                            slot="editor"
                            type="text"
                            .value=${size}
                            @input=${this.#onSizeChange}
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
     * state and loses the empty "provider default" entry.
     */
    #getSizeOptions(sizes: string[], selected: string): Array<{ name: string; value: string; selected?: boolean }> {
        const cacheKey = `${sizes.join(",")}|${selected}`;
        if (this.#cachedOptionsKey === cacheKey) return this.#cachedOptions!;

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

        this.#cachedOptionsKey = cacheKey;
        this.#cachedOptions = options;

        return options;
    }

    #cachedOptionsKey?: string;
    #cachedOptions?: Array<{ name: string; value: string; selected?: boolean }>;

    override render() {
        const [mediaTypeRule] = UAI_IMAGE_GENERATION_SETTING_RULES;

        return html`
            <uui-box headline="System Settings">
                ${this.#renderSize()}
                ${isRuleSupported(mediaTypeRule, this.metadata) ? this.#renderMediaType() : nothing}
            </uui-box>
        `;
    }

    #renderMediaType() {
        return html`
            <umb-property-layout label="Media Type" description="Output image encoding (e.g. &quot;image/png&quot;, &quot;image/jpeg&quot;, &quot;image/webp&quot;). Supported values vary by model.">
                <uui-input
                    slot="editor"
                    type="text"
                    .value=${this.settings?.mediaType ?? ""}
                    @input=${this.#onMediaTypeChange}
                    placeholder="Provider default"
                ></uui-input>
            </umb-property-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-input,
            uui-select {
                width: 100%;
            }
        `,
    ];
}

export default UaiImageGenerationProfileSettingsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-image-generation-profile-settings": UaiImageGenerationProfileSettingsElement;
    }
}
