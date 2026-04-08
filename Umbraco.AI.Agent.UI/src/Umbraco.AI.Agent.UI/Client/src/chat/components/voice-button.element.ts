import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_NOTIFICATION_CONTEXT, type UmbNotificationContext } from "@umbraco-cms/backoffice/notification";
import { UaiAudioRecorder, UaiSpeechToTextController } from "@umbraco-ai/core";

type VoiceButtonState = "idle" | "recording" | "transcribing";

/**
 * Voice recording button for speech-to-text transcription.
 *
 * Records audio via {@link UaiAudioRecorder} and transcribes it via
 * {@link UaiSpeechToTextController}. On success fires a `transcription` custom event.
 *
 * @fires transcription - Dispatched when transcription succeeds. detail: { text: string }
 */
@customElement("uai-voice-button")
export class UaiVoiceButtonElement extends UmbLitElement {
    /** Optional AI profile ID or alias to use for transcription. */
    @property({ type: String, attribute: "profile-id-or-alias" })
    profileIdOrAlias?: string;

    /** Optional BCP-47 language hint (e.g., "en", "de"). */
    @property({ type: String })
    language?: string;

    /** Whether the button should be disabled. */
    @property({ type: Boolean })
    disabled = false;

    @state()
    private _state: VoiceButtonState = "idle";

    #recorder = new UaiAudioRecorder();
    #sttController = new UaiSpeechToTextController(this);
    #notificationContext?: UmbNotificationContext;

    constructor() {
        super();
        this.observe(this.#recorder.state$, (s) => { this._state = s; });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });
    }

    async #handleClick() {
        if (this.disabled) return;

        if (this._state === "idle") {
            await this.#startRecording();
        } else if (this._state === "recording") {
            await this.#stopAndTranscribe();
        }
    }

    async #startRecording() {
        try {
            await this.#recorder.start();
        } catch {
            this.#notificationContext?.peek("danger", {
                data: {
                    headline: "Microphone access denied",
                    message: "Please allow microphone access to use voice input.",
                },
            });
        }
    }

    async #stopAndTranscribe() {
        try {
            const audioBlob = await this.#recorder.stop();

            this._state = "transcribing";
            const { data, error } = await this.#sttController.transcribe(audioBlob, {
                profileIdOrAlias: this.profileIdOrAlias,
                language: this.language,
            });

            if (error) {
                throw new Error(
                    (error as { detail?: string })?.detail ?? "Transcription failed.",
                );
            }

            this.dispatchEvent(
                new CustomEvent("transcription", {
                    detail: { text: data?.text ?? "" },
                    bubbles: true,
                    composed: true,
                }),
            );
        } catch (err) {
            this.#notificationContext?.peek("danger", {
                data: {
                    headline: "Transcription failed",
                    message: err instanceof Error ? err.message : "An error occurred during transcription.",
                },
            });
        } finally {
            this._state = "idle";
        }
    }

    override render() {
        const isRecording = this._state === "recording";
        const isTranscribing = this._state === "transcribing";

        const title = isRecording
            ? "Stop recording"
            : isTranscribing
              ? "Transcribing..."
              : "Record voice message";

        return html`
            <uui-button
                compact
                look="secondary"
                ?disabled=${this.disabled || isTranscribing}
                class=${isRecording ? "recording" : ""}
                title=${title}
                @click=${this.#handleClick}
            >
                ${isTranscribing
                    ? html`<uui-loader-circle></uui-loader-circle>`
                    : html`<uui-icon name="icon-mic"></uui-icon>`}
            </uui-button>
        `;
    }

    static override styles = css`
        :host {
            display: contents;
        }

        uui-button.recording {
            --uui-button-background-color: var(--uui-color-danger);
            --uui-button-background-color-hover: var(--uui-color-danger-emphasis);
            --uui-button-contrast: var(--uui-color-danger-contrast);
            animation: pulse 1.5s ease-in-out infinite;
        }

        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.65; }
        }
    `;
}

export default UaiVoiceButtonElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-voice-button": UaiVoiceButtonElement;
    }
}
