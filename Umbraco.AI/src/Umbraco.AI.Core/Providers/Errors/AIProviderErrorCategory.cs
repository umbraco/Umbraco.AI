namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Categorises an error returned by an AI provider SDK for UX and retry decisions.
/// </summary>
/// <remarks>
/// Classifies provider-response failures (HTTP/SSE errors thrown by the provider SDK).
/// Distinct from <c>AIAuditLogErrorCategory</c>, which covers broader application-layer
/// failure modes such as guardrails, tool execution, and context resolution.
/// </remarks>
public enum AIProviderErrorCategory
{
    /// <summary>
    /// Unrecognised error. The classifier could not match it to a known category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Transient server-side condition. Safe to retry after a short backoff.
    /// Examples: Anthropic <c>overloaded_error</c>, HTTP 502/503/504, 529.
    /// </summary>
    Transient = 1,

    /// <summary>
    /// Caller exceeded a rate limit or quota. Retry after the suggested delay.
    /// Maps to HTTP 429.
    /// </summary>
    RateLimited = 2,

    /// <summary>
    /// Authentication or authorisation failure. The connection's credentials are missing,
    /// invalid, or lack the required permissions. Maps to HTTP 401/403.
    /// </summary>
    Authentication = 3,

    /// <summary>
    /// The request was rejected as invalid (bad payload, unsupported model, oversized prompt).
    /// Retrying without changes will not help. Maps to HTTP 400.
    /// </summary>
    InvalidRequest = 4,

    /// <summary>
    /// The requested resource (e.g. model) was not found. Maps to HTTP 404.
    /// </summary>
    NotFound = 5,

    /// <summary>
    /// The operation was cancelled (timeout or caller cancellation).
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Connectivity failure before the server could respond (DNS, refused connection, TLS).
    /// </summary>
    NetworkError = 7,
}
