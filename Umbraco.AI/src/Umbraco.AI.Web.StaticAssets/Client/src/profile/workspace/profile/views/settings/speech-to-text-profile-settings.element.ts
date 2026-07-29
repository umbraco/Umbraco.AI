import { css, html, customElement, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiSpeechToTextProfileSettings } from "../../../../types.js";
import { isRuleSupported } from "./declared-settings.js";
import { UAI_SPEECH_TO_TEXT_SETTING_RULES } from "./rules.js";
import { uaiProfileSettingsChangeEvent } from "./profile-settings-change.event.js";

/**
 * Speech-to-text profile settings.
 */
@customElement("uai-speech-to-text-profile-settings")
export class UaiSpeechToTextProfileSettingsElement extends UmbLitElement {
    @property({ type: Object })
    settings: UaiSpeechToTextProfileSettings | null = null;

    /** The selected model descriptor's metadata, carrying the provider's per-model declarations. */
    @property({ type: Object })
    metadata?: Record<string, string>;

    #onLanguageChange(event: Event) {
        event.stopPropagation();
        this.dispatchEvent(uaiProfileSettingsChangeEvent<UaiSpeechToTextProfileSettings>({
            $type: "speechToText",
            language: (event.target as HTMLInputElement).value || null,
        }));
    }

    override render() {
        const [language] = UAI_SPEECH_TO_TEXT_SETTING_RULES;

        // Not every transcription model takes a language hint, and it is the only setting here.
        if (!isRuleSupported(language, this.metadata)) return nothing;

        return html`
            <uui-box headline="System Settings">
                <umb-property-layout
                    label="Language"
                    description="BCP-47 language hint for transcription (e.g., &quot;en&quot;, &quot;de&quot;, &quot;ja&quot;). Leave empty for auto-detection."
                >
                    <uui-input
                        slot="editor"
                        type="text"
                        .value=${this.settings?.language ?? ""}
                        @input=${this.#onLanguageChange}
                        placeholder="Auto-detect"
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

export default UaiSpeechToTextProfileSettingsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-speech-to-text-profile-settings": UaiSpeechToTextProfileSettingsElement;
    }
}
