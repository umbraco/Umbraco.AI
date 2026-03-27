import { BehaviorSubject, type Observable } from '@umbraco-cms/backoffice/external/rxjs';
import { SpeechToTextService } from '../../api/index.js';

export type SpeechToTextRecorderState = "idle" | "recording" | "transcribing";

export interface SpeechToTextRecorderConfig {
    /** Optional AI profile ID or alias to use for transcription. */
    profileIdOrAlias?: string;
    /** Optional BCP-47 language hint (e.g., "en", "de"). */
    language?: string;
}

/** Audio MIME type to use for recording — webm/opus is well-supported in modern browsers. */
const PREFERRED_MIME_TYPE = "audio/webm;codecs=opus";
const FALLBACK_MIME_TYPE = "audio/webm";

/**
 * Self-contained speech-to-text recorder that handles both audio recording
 * and transcription via the Umbraco AI API.
 *
 * Uses the generated `SpeechToTextService` hey-api client for transcription,
 * which handles authentication automatically via the configured singleton client.
 *
 * State is exposed as an observable so Lit elements can bind to it with `this.observe()`.
 *
 * @example
 * ```ts
 * const recorder = new SpeechToTextRecorder();
 * this.observe(recorder.state$, (state) => { this._state = state; });
 * await recorder.startRecording();
 * const text = await recorder.stopAndTranscribe();
 * ```
 */
export class SpeechToTextRecorder {
    #mediaRecorder?: MediaRecorder;
    #chunks: Blob[] = [];
    #state$ = new BehaviorSubject<SpeechToTextRecorderState>("idle");
    #config: SpeechToTextRecorderConfig;

    /** Observable of the current recorder state. */
    get state$(): Observable<SpeechToTextRecorderState> {
        return this.#state$.asObservable();
    }

    /** Current state (synchronous read). */
    get state(): SpeechToTextRecorderState {
        return this.#state$.value;
    }

    constructor(config: SpeechToTextRecorderConfig = {}) {
        this.#config = config;
    }

    /**
     * Request microphone access and begin recording.
     * @throws Error if microphone access is denied.
     */
    async startRecording(): Promise<void> {
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
        this.#state$.next("recording");
    }

    /**
     * Stop recording and transcribe the audio via the Umbraco AI API.
     * @returns The transcribed text.
     * @throws Error if recording or transcription fails.
     */
    async stopAndTranscribe(): Promise<string> {
        if (!this.#mediaRecorder) {
            throw new Error("No active recording to stop.");
        }

        this.#state$.next("transcribing");

        const audioBlob = await this.#stopRecorder();

        try {
            const { data, error } = await SpeechToTextService.transcribeAudio({
                body: { audioFile: audioBlob },
                query: {
                    profileIdOrAlias: this.#config.profileIdOrAlias,
                    language: this.#config.language,
                },
            });

            if (error) {
                throw new Error(
                    (error as { detail?: string })?.detail
                    ?? "Transcription request failed.",
                );
            }

            return data?.text ?? "";
        } finally {
            this.#state$.next("idle");
        }
    }

    /** Cancel any active recording without transcribing. */
    cancel(): void {
        if (this.#mediaRecorder && this.#mediaRecorder.state !== "inactive") {
            this.#mediaRecorder.stream.getTracks().forEach((t) => t.stop());
            this.#mediaRecorder.stop();
        }
        this.#mediaRecorder = undefined;
        this.#chunks = [];
        this.#state$.next("idle");
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
}
