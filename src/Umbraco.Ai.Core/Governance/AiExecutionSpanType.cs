namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Represents the type of execution span within an AI trace.
/// </summary>
public enum AiExecutionSpanType
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
