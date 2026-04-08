import { BehaviorSubject, type Observable } from '@umbraco-cms/backoffice/external/rxjs';

export type UaiAudioRecorderState = "idle" | "recording";

/** Audio MIME type to use for recording — webm/opus is well-supported in modern browsers. */
const PREFERRED_MIME_TYPE = "audio/webm;codecs=opus";
const FALLBACK_MIME_TYPE = "audio/webm";

/**
 * Manages browser audio recording via the MediaRecorder API.
 *
 * Handles microphone access, chunk collection, and track cleanup.
 * Returns the recorded audio as a `Blob` — transcription is the caller's responsibility
 * via {@link UaiSpeechToTextController}.
 *
 * State is exposed as an observable so Lit elements can bind to it with `this.observe()`.
 *
 * @example
 * ```ts
 * const recorder = new UaiAudioRecorder();
 * this.observe(recorder.state$, (state) => { this._recorderState = state; });
 * await recorder.start();
 * const audioBlob = await recorder.stop();
 * const { data } = await sttController.transcribe(audioBlob);
 * ```
 *
 * @public
 */
export class UaiAudioRecorder {
    #mediaRecorder?: MediaRecorder;
    #chunks: Blob[] = [];
    #state$ = new BehaviorSubject<UaiAudioRecorderState>("idle");

    /** Observable of the current recorder state. */
    get state$(): Observable<UaiAudioRecorderState> {
        return this.#state$.asObservable();
    }

    /** Current state (synchronous read). */
    get state(): UaiAudioRecorderState {
        return this.#state$.value;
    }

    /**
     * Request microphone access and begin recording.
     * @throws Error if microphone access is denied.
     */
    async start(): Promise<void> {
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
     * Stop recording and return the captured audio.
     * @returns The recorded audio as a Blob.
     * @throws Error if no active recording exists.
     */
    async stop(): Promise<Blob> {
        if (!this.#mediaRecorder) {
            throw new Error("No active recording to stop.");
        }

        const blob = await this.#stopRecorder();
        this.#state$.next("idle");
        return blob;
    }

    /** Cancel any active recording without returning audio. */
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
