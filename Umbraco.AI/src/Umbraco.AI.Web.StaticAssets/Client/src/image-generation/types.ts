/**
 * A single generated image, returned as base64 data and/or a URL.
 * @public
 */
export interface UaiGeneratedImage {
    /** Base64-encoded image data, when the provider returned inline data. */
    data?: string;
    /** Image URL, when the provider returned a hosted/URI image. */
    url?: string;
    /** Media type of the image (e.g. "image/png"). */
    mediaType?: string;
}

/**
 * Result of an image-generation request.
 * @public
 */
export interface UaiImageGenerationResult {
    images: UaiGeneratedImage[];
    usage?: {
        inputTokens?: number;
        outputTokens?: number;
        totalTokens?: number;
    };
}

/**
 * A base64-encoded input image for maskless editing.
 * @public
 */
export interface UaiImageInput {
    data: string;
    mediaType: string;
}

/**
 * Options for image generation (public API).
 * @public
 */
export interface UaiImageGenerationOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default image-generation profile. */
    profileIdOrAlias?: string;
    /** Number of images to generate. */
    count?: number;
    /** Image size as "{width}x{height}" (e.g. "1024x1024"). */
    size?: string;
    /** Response format: "url", "data", or "hosted". */
    responseFormat?: string;
    /** Original images to edit (maskless edit). */
    originalImages?: UaiImageInput[];
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiImageGenerationRequest {
    prompt: string;
    profileIdOrAlias?: string | null;
    count?: number | null;
    size?: string | null;
    responseFormat?: string | null;
    originalImages?: UaiImageInput[] | null;
    signal?: AbortSignal;
}
