namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Represents the type of execution span within an AI audit.
/// </summary>
public enum AiAuditActivityType
{
    /// <summary>
    /// Profile resolution span.
    /// </summary>
    ProfileResolution = 0,

    /// <summary>
    /// Context resolution span.
    /// </summary>
    ContextResolution = 1,

    /// <summary>
    /// Model API call span.
    /// </summary>
    ModelCall = 2,

    /// <summary>
    /// Tool invocation span.
    /// </summary>
    ToolInvocation = 3,

    /// <summary>
    /// Middleware execution span.
    /// </summary>
    Middleware = 4
}
