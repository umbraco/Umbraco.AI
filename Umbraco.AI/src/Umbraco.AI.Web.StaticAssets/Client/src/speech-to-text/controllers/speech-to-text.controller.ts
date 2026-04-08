import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiSpeechToTextRepository } from "../repository/speech-to-text.repository.js";
import type { UaiTranscriptionOptions, UaiTranscriptionResult } from "../types.js";

/**
 * Public API for transcribing audio to text.
 * @public
 */
export class UaiSpeechToTextController extends UmbControllerBase {
    #repository: UaiSpeechToTextRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiSpeechToTextRepository(host);
    }

    /**
     * Transcribes an audio file to text.
     * @param audioFile - The audio blob to transcribe.
     * @param options - Optional configuration (profile ID/alias, language, abort signal).
     * @returns The transcription result or error.
     */
    async transcribe(
        audioFile: Blob,
        options?: UaiTranscriptionOptions,
    ): Promise<{ data?: UaiTranscriptionResult; error?: unknown }> {
        return this.#repository.transcribe({
            profileIdOrAlias: options?.profileIdOrAlias,
            language: options?.language,
            audioFile,
            signal: options?.signal,
        });
    }
}
