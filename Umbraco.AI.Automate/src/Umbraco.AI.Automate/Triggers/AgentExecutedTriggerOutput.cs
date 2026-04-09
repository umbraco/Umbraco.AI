namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Output produced by the <see cref="AgentExecutedTrigger"/> for each agent execution.
/// </summary>
public sealed class AgentExecutedTriggerOutput
{
    /// <summary>
    /// Gets the agent's unique identifier.
    /// </summary>
    public Guid AgentId { get; init; }

    /// <summary>
    /// Gets the agent's alias.
    /// </summary>
    public string AgentAlias { get; init; } = string.Empty;

    /// <summary>
    /// Gets the agent's display name.
    /// </summary>
    public string AgentName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
}
