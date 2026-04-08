/**
 * Result of a speech-to-text transcription.
 * @public
 */
export interface UaiTranscriptionResult {
    text: string;
}

/**
 * Options for speech-to-text transcription (public API).
 * @public
 */
export interface UaiTranscriptionOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default speech-to-text profile. */
    profileIdOrAlias?: string;
    /** BCP-47 language hint (e.g., "en", "de"). */
    language?: string;
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiTranscriptionRequest {
    profileIdOrAlias?: string | null;
    language?: string | null;
    audioFile: Blob;
    signal?: AbortSignal;
}
