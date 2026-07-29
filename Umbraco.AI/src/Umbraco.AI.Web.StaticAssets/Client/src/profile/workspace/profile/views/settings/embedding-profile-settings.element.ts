import { css, html, customElement, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiEmbeddingProfileSettings } from "../../../../types.js";
import { isRuleSupported } from "./declared-settings.js";
import { UAI_EMBEDDING_SETTING_RULES } from "./rules.js";
import { uaiProfileSettingsChangeEvent } from "./profile-settings-change.event.js";

/**
 * Embedding profile settings.
 */
@customElement("uai-embedding-profile-settings")
export class UaiEmbeddingProfileSettingsElement extends UmbLitElement {
    @property({ type: Object })
    settings: UaiEmbeddingProfileSettings | null = null;

    /** The selected model descriptor's metadata, carrying the provider's per-model declarations. */
    @property({ type: Object })
    metadata?: Record<string, string>;

    #onDimensionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#update({ dimensions: value ? parseInt(value, 10) : null });
    }

    #update(updates: Partial<UaiEmbeddingProfileSettings>) {
        this.dispatchEvent(uaiProfileSettingsChangeEvent<UaiEmbeddingProfileSettings>({
            $type: "embedding",
            dimensions: this.settings?.dimensions ?? null,
            ...updates,
        }));
    }

    override render() {
        const [dimensions] = UAI_EMBEDDING_SETTING_RULES;

        // Shortened embeddings are a text-embedding-3 feature. Dimensions is the only setting here, so when
        // the model rejects it the whole panel goes rather than leaving an empty box.
        if (!isRuleSupported(dimensions, this.metadata)) return nothing;

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
                        .value=${this.settings?.dimensions?.toString() ?? ""}
                        @input=${this.#onDimensionsChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-input {
                width: 100%;
            }
        `,
    ];
}

export default UaiEmbeddingProfileSettingsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-embedding-profile-settings": UaiEmbeddingProfileSettingsElement;
    }
}
