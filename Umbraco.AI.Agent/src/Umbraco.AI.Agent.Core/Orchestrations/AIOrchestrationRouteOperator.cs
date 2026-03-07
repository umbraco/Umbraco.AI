namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Comparison operators for <see cref="AIOrchestrationRouteCondition"/>.
/// </summary>
public enum AIOrchestrationRouteOperator
{
    /// <summary>
    /// Exact string equality (case-insensitive).
    /// </summary>
    Equals,

    /// <summary>
    /// Substring match (case-insensitive).
    /// </summary>
    Contains,

    /// <summary>
    /// Prefix match (case-insensitive).
    /// </summary>
    StartsWith,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    Matches,
}
