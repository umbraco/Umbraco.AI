namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Categorizes the type of error that occurred during an AI operation.
/// </summary>
public enum AiTraceErrorCategory
{
    /// <summary>
    /// Authentication or authorization failure.
    /// </summary>
    Authentication = 0,

    /// <summary>
    /// Rate limiting or quota exceeded error.
    /// </summary>
    RateLimiting = 1,

    /// <summary>
    /// The requested model was not found or is unavailable.
    /// </summary>
    ModelNotFound = 2,

    /// <summary>
    /// The request was invalid or malformed.
    /// </summary>
    InvalidRequest = 3,

    /// <summary>
    /// The AI service returned a server error.
    /// </summary>
    ServerError = 4,

    /// <summary>
    /// A network error occurred while communicating with the AI service.
    /// </summary>
    NetworkError = 5,

    /// <summary>
    /// Failed to resolve context for the AI operation.
    /// </summary>
    ContextResolution = 6,

    /// <summary>
    /// Error occurred during tool execution.
    /// </summary>
    ToolExecution = 7,

    /// <summary>
    /// Unknown or uncategorized error.
    /// </summary>
    Unknown = 99
}
