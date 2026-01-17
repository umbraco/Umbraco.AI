/**
 * Chat message role.
 * @public
 */
export type UaiChatRole = 'user' | 'assistant' | 'system';

/**
 * Represents a chat message.
 * @public
 */
export interface UaiChatMessage {
    role: UaiChatRole;
    content: string;
}

/**
 * Token usage statistics.
 * @public
 */
export interface UaiChatUsage {
    inputTokens?: number | null;
    outputTokens?: number | null;
    totalTokens?: number | null;
}

/**
 * Result of a chat completion.
 * @public
 */
export interface UaiChatResult {
    message: UaiChatMessage;
    finishReason?: string | null;
    usage?: UaiChatUsage | null;
}

/**
 * Streaming chunk from chat completion.
 * @public
 */
export interface UaiChatStreamChunk {
    content: string;
    finishReason?: string | null;
}

/**
 * Internal request model for repository/data source.
 * @internal
 */
export interface UaiChatRequest {
    profileIdOrAlias?: string | null;
    messages: UaiChatMessage[];
    signal?: AbortSignal;
}

/**
 * Options for chat completion (public API).
 * @public
 */
export interface UaiChatOptions {
    /** Profile ID (GUID) or alias. If omitted, uses the default chat profile. */
    profileIdOrAlias?: string;
    /** AbortSignal for cancellation. */
    signal?: AbortSignal;
}
