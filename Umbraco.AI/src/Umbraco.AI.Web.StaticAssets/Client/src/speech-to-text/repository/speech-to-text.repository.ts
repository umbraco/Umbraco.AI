import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UaiSpeechToTextServerDataSource } from "./speech-to-text.server.data-source.js";
import type { UaiTranscriptionRequest, UaiTranscriptionResult } from "../types.js";

/**
 * Repository for speech-to-text operations.
 */
export class UaiSpeechToTextRepository extends UmbControllerBase {
    #dataSource: UaiSpeechToTextServerDataSource;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiSpeechToTextServerDataSource(host);
    }

    /**
     * Transcribes an audio file to text.
     */
    async transcribe(request: UaiTranscriptionRequest): Promise<{ data?: UaiTranscriptionResult; error?: unknown }> {
        return this.#dataSource.transcribe(request);
    }
}
