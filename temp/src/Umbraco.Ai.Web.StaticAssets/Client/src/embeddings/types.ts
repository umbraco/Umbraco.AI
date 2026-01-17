/**
 * A single embedding vector with its index.
 * @public
 */
export interface UaiEmbeddingItem {
    index: number;
    vector: number[];
}

/**
 * Result of an embedding generation request.
 * @public
 */
export interface UaiEmbeddingResult {
    embeddings: UaiEmbeddingItem[];
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiEmbeddingRequest {
    profileId?: string | null;
    values: string[];
    signal?: AbortSignal;
}

/**
 * Options for embedding generation (public API).
 * @public
 */
export interface UaiEmbeddingOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default embedding profile. */
    profile?: string;
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}
