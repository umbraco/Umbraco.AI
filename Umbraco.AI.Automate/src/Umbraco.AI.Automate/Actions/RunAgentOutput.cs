namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Output produced by the <see cref="RunAgentAction"/>.
/// </summary>
public sealed class RunAgentOutput
{
    /// <summary>
    /// Gets the ID of the agent that was executed.
    /// </summary>
    public Guid AgentId { get; init; }

    /// <summary>
    /// Gets the alias of the agent that was executed.
    /// </summary>
    public string AgentAlias { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the agent execution completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the text response from the agent (last assistant message).
    /// </summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>
    /// Gets the execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }
}
