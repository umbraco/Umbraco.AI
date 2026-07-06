import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { ImageGenerationService } from "../../api/sdk.gen.js";
import type { UaiImageGenerationRequest, UaiImageGenerationResult } from "../types.js";

/**
 * Server data source for image-generation operations.
 */
export class UaiImageGenerationServerDataSource {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Generates one or more images from a text prompt.
     */
    async generate(request: UaiImageGenerationRequest): Promise<{ data?: UaiImageGenerationResult; error?: unknown }> {
        const { data, error } = await tryExecute(
            this.#host,
            ImageGenerationService.generate({
                body: {
                    prompt: request.prompt,
                    profileIdOrAlias: request.profileIdOrAlias ?? undefined,
                    count: request.count ?? undefined,
                    size: request.size ?? undefined,
                    responseFormat: request.responseFormat ?? undefined,
                    originalImages: request.originalImages ?? undefined,
                },
                signal: request.signal,
            }),
        );

        if (error || !data) {
            return { error };
        }

        return {
            data: {
                images: (data.images ?? []).map((image) => ({
                    data: image.data ?? undefined,
                    url: image.url ?? undefined,
                    mediaType: image.mediaType ?? undefined,
                })),
                usage: data.usage
                    ? {
                          inputTokens: data.usage.inputTokens ?? undefined,
                          outputTokens: data.usage.outputTokens ?? undefined,
                          totalTokens: data.usage.totalTokens ?? undefined,
                      }
                    : undefined,
            },
        };
    }
}
