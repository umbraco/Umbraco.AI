import { BehaviorSubject, type Observable } from '@umbraco-cms/backoffice/external/rxjs';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UaiSpeechToTextController } from '../../speech-to-text/controllers/speech-to-text.controller.js';

export type UaiSpeechToTextRecorderState = "idle" | "recording" | "transcribing";

export interface UaiSpeechToTextRecorderConfig {
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
 * and transcription via the {@link UaiSpeechToTextController}.
 *
 * State is exposed as an observable so Lit elements can bind to it with `this.observe()`.
 *
 * @example
 * ```ts
 * const recorder = new UaiSpeechToTextRecorder(this);
 * this.observe(recorder.state$, (state) => { this._state = state; });
 * await recorder.startRecording();
 * const text = await recorder.stopAndTranscribe();
 * ```
 *
 * @public
 */
export class UaiSpeechToTextRecorder {
    #controller: UaiSpeechToTextController;
    #mediaRecorder?: MediaRecorder;
    #chunks: Blob[] = [];
    #state$ = new BehaviorSubject<UaiSpeechToTextRecorderState>("idle");
    #config: UaiSpeechToTextRecorderConfig;

    /** Observable of the current recorder state. */
    get state$(): Observable<UaiSpeechToTextRecorderState> {
        return this.#state$.asObservable();
    }

    /** Current state (synchronous read). */
    get state(): UaiSpeechToTextRecorderState {
        return this.#state$.value;
    }

    constructor(host: UmbControllerHost, config: UaiSpeechToTextRecorderConfig = {}) {
        this.#controller = new UaiSpeechToTextController(host);
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
            const { data, error } = await this.#controller.transcribe(audioBlob, {
                profileIdOrAlias: this.#config.profileIdOrAlias,
                language: this.#config.language,
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
