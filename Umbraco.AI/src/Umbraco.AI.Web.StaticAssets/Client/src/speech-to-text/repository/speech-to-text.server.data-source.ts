import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { SpeechToTextService } from "../../api/sdk.gen.js";
import type { UaiSpeechToTextRequest, UaiSpeechToTextResult } from "../types.js";

/**
 * Server data source for speech-to-text operations.
 */
export class UaiSpeechToTextServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Transcribes an audio file to text.
     */
    async transcribe(request: UaiSpeechToTextRequest): Promise<{ data?: UaiSpeechToTextResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            SpeechToTextService.transcribeAudio({
                body: { audioFile: request.audioFile },
                query: {
                    profileIdOrAlias: request.profileIdOrAlias ?? undefined,
                    language: request.language ?? undefined,
                },
                signal: request.signal,
            }),
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                text: data.text ?? "",
            },
        };
    }
}
