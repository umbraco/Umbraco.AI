import { css, html, customElement, property, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UaiChatProfileSettings } from "../../../../types.js";
import { isRuleSupported } from "./declared-settings.js";
import { UAI_CHAT_SETTING_RULES } from "./rules.js";
import { uaiProfileSettingsChangeEvent } from "./profile-settings-change.event.js";

/**
 * Chat profile settings: temperature, max tokens, system prompt and contexts.
 */
@customElement("uai-chat-profile-settings")
export class UaiChatProfileSettingsElement extends UmbLitElement {
    @property({ type: Object })
    settings: UaiChatProfileSettings | null = null;

    /** The selected model descriptor's metadata, carrying the provider's per-model declarations. */
    @property({ type: Object })
    metadata?: Record<string, string>;

    #observedEditor?: Element;
    #observedEditorWidth = 0;

    /**
     * Keeps the temperature slider's step markers aligned with its track.
     *
     * `uui-slider` measures the track once, when it first renders, and thereafter only when the window
     * resizes — so any later width change leaves the markers spaced for the old width, running past the end
     * of the track. A scrollbar appearing as the editor fills out is enough to trigger it. Re-firing the
     * event the slider already listens for is the supported way to make it measure again.
     *
     * The container is observed rather than the slider itself: `umb-input-slider` declares no display on its
     * host, so it is an inline element, and a ResizeObserver reports nothing for those.
     */
    #resizeObserver = new ResizeObserver((entries) => {
        const width = Math.round(entries[0]?.contentRect.width ?? 0);
        if (width === 0 || width === this.#observedEditorWidth) return;

        this.#observedEditorWidth = width;
        window.dispatchEvent(new Event("resize"));
    });

    override disconnectedCallback() {
        this.#resizeObserver.disconnect();
        super.disconnectedCallback();
    }

    protected override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);

        const editor = this.shadowRoot?.querySelector(".temperature-editor") ?? undefined;
        if (editor === this.#observedEditor) return;

        if (this.#observedEditor) this.#resizeObserver.unobserve(this.#observedEditor);

        this.#observedEditor = editor;
        this.#observedEditorWidth = 0;
        if (editor) this.#resizeObserver.observe(editor);
    }

    #onTemperatureChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#update({ temperature: value ? parseFloat(value) : null });
    }

    /** Returns temperature to unset, so the provider's own default applies again. */
    #onTemperatureClear(event: Event) {
        event.stopPropagation();
        this.#update({ temperature: null });
    }

    #onMaxTokensChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#update({ maxTokens: value ? parseInt(value, 10) : null });
    }

    #onSystemPromptChange(event: Event) {
        event.stopPropagation();
        this.#update({ systemPromptTemplate: (event.target as HTMLTextAreaElement).value || null });
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#update({ contextIds: picker.value ?? [] });
    }

    #update(updates: Partial<UaiChatProfileSettings>) {
        this.dispatchEvent(uaiProfileSettingsChangeEvent<UaiChatProfileSettings>({
            $type: "chat",
            temperature: this.settings?.temperature ?? null,
            maxTokens: this.settings?.maxTokens ?? null,
            systemPromptTemplate: this.settings?.systemPromptTemplate ?? null,
            contextIds: this.settings?.contextIds ?? [],
            guardrailIds: this.settings?.guardrailIds ?? [],
            ...updates,
        }));
    }

    /**
     * Renders the temperature control in one of three states: rejected by the selected model, unset (so the
     * provider's own default applies), or an explicit value.
     *
     * The slider cannot express "unset" on its own — with no value it parks at its minimum, which reads as a
     * deliberate 0 — so an unset slider is dimmed, and the clear button beside it is how the value gets given
     * back. Nothing is stored until the slider moves, so an untouched profile keeps its null.
     */
    #renderTemperature() {
        const [temperatureRule] = UAI_CHAT_SETTING_RULES;
        if (!isRuleSupported(temperatureRule, this.metadata)) return nothing;

        const temperature = this.settings?.temperature ?? null;

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

    override render() {
        return html`
            <uui-box headline="System Settings">
                ${this.#renderTemperature()}

                <umb-property-layout label="Max Tokens" description="Maximum number of tokens to generate">
                    <uui-input
                        slot="editor"
                        type="number"
                        min="1"
                        .value=${this.settings?.maxTokens?.toString() ?? ""}
                        @input=${this.#onMaxTokensChange}
                        placeholder="Default"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="System Prompt" description="System prompt template for this profile">
                    <uui-textarea
                        slot="editor"
                        .value=${this.settings?.systemPromptTemplate ?? ""}
                        @input=${this.#onSystemPromptChange}
                        placeholder="Enter system prompt template..."
                        rows="6"
                    ></uui-textarea>
                </umb-property-layout>

                <umb-property-layout label="Contexts" description="Predefined contexts to include in chat sessions">
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${this.settings?.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
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

            uui-input,
            uui-textarea,
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
        `,
    ];
}

export default UaiChatProfileSettingsElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-chat-profile-settings": UaiChatProfileSettingsElement;
    }
}
