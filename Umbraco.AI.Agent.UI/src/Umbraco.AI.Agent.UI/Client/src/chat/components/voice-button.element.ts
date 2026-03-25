import { customElement, property, state, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import { UMB_NOTIFICATION_CONTEXT, type UmbNotificationContext } from "@umbraco-cms/backoffice/notification";

type VoiceButtonState = "idle" | "recording" | "transcribing";

/** Audio MIME type to use for recording — webm/opus is well-supported in modern browsers. */
const PREFERRED_MIME_TYPE = "audio/webm;codecs=opus";
const FALLBACK_MIME_TYPE = "audio/webm";

/**
 * Voice recording button for speech-to-text transcription.
 *
 * Records audio via MediaRecorder API and sends it to the Umbraco AI
 * speech-to-text endpoint. On success fires a `transcription` custom event.
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

    #mediaRecorder?: MediaRecorder;
    #chunks: Blob[] = [];
    #authToken?: string | (() => string | Promise<string | undefined>);
    #baseUrl = "";
    #credentials: RequestCredentials = "same-origin";
    #notificationContext?: UmbNotificationContext;

    constructor() {
        super();
        this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
            const config = authContext?.getOpenApiConfiguration();
            if (config) {
                this.#authToken = config.token ?? undefined;
                this.#baseUrl = config.base ?? "";
                this.#credentials = (config.credentials as RequestCredentials) ?? "same-origin";
            }
        });
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
        // If transcribing, ignore click
    }

    async #startRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

            const mimeType = MediaRecorder.isTypeSupported(PREFERRED_MIME_TYPE)
                ? PREFERRED_MIME_TYPE
                : FALLBACK_MIME_TYPE;

            this.#chunks = [];
            this.#mediaRecorder = new MediaRecorder(stream, { mimeType });

            this.#mediaRecorder.ondataavailable = (e) => {
                if (e.data.size > 0) {
                    this.#chunks.push(e.data);
                }
            };

            this.#mediaRecorder.start();
            this._state = "recording";
        } catch (err) {
            this.#notificationContext?.peek("danger", {
                data: {
                    headline: "Microphone access denied",
                    message: "Please allow microphone access to use voice input.",
                },
            });
        }
    }

    async #stopAndTranscribe() {
        if (!this.#mediaRecorder) return;

        this._state = "transcribing";

        const audioBlob = await this.#stopRecorder();

        try {
            const text = await this.#transcribe(audioBlob);
            this.dispatchEvent(
                new CustomEvent("transcription", {
                    detail: { text },
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

    #stopRecorder(): Promise<Blob> {
        return new Promise((resolve) => {
            this.#mediaRecorder!.onstop = () => {
                const mimeType = this.#mediaRecorder!.mimeType || "audio/webm";
                const blob = new Blob(this.#chunks, { type: mimeType });

                // Stop all tracks to release microphone
                this.#mediaRecorder!.stream.getTracks().forEach((t) => t.stop());
                this.#mediaRecorder = undefined;
                this.#chunks = [];

                resolve(blob);
            };
            this.#mediaRecorder!.stop();
        });
    }

    async #transcribe(audioBlob: Blob): Promise<string> {
        const formData = new FormData();
        formData.append("audioFile", audioBlob, "recording.webm");

        const url = new URL(`${this.#baseUrl}/umbraco/ai/management/api/v1/speech-to-text/transcribe`);
        if (this.profileIdOrAlias) {
            url.searchParams.set("profileIdOrAlias", this.profileIdOrAlias);
        }
        if (this.language) {
            url.searchParams.set("language", this.language);
        }

        const headers: Record<string, string> = {};
        const rawToken = typeof this.#authToken === "function"
            ? await this.#authToken()
            : this.#authToken;
        if (rawToken) {
            headers["Authorization"] = `Bearer ${rawToken}`;
        }

        const response = await fetch(url.toString(), {
            method: "POST",
            body: formData,
            headers,
            credentials: this.#credentials,
        });

        if (!response.ok) {
            const problem = await response.json().catch(() => null);
            throw new Error(problem?.detail ?? `Transcription request failed (${response.status})`);
        }

        const result = await response.json() as { text: string };
        return result.text;
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
                    : html`<uui-icon name="icon-microphone"></uui-icon>`}
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
