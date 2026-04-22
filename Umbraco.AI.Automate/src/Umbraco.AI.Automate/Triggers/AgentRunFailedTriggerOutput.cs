using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Output produced when an AI agent run fails.
/// </summary>
public sealed class AgentRunFailedTriggerOutput
{
    /// <summary>
    /// Gets the ID of the agent that ran.
    /// </summary>
    [Field(Label = "Agent ID", Description = "The unique identifier of the agent that ran.")]
    public Guid AgentId { get; init; }

    /// <summary>
    /// Gets the URL-safe alias of the agent that ran.
    /// </summary>
    [Field(Label = "Agent Alias", Description = "The URL-safe alias of the agent.")]
    public string AgentAlias { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name of the agent that ran.
    /// </summary>
    [Field(Label = "Agent Name", Description = "The display name of the agent.")]
    public string AgentName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the final user message sent to the agent.
    /// </summary>
    [Field(Label = "Prompt", Description = "The final user message sent to the agent.")]
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total execution time in seconds.
    /// </summary>
    [Field(Label = "Duration (seconds)", Description = "Total execution time before failure, in seconds.")]
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Gets a description of the failure. When an exception was captured its message is used,
    /// otherwise the first error event message, or a generic fallback.
    /// </summary>
    [Field(Label = "Error Message", Description = "Description of the failure.")]
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full type name of the exception that caused the failure, if an exception was captured.
    /// </summary>
    [Field(Label = "Error Type", Description = "The .NET type of the captured exception, if any.")]
    public string? ErrorType { get; init; }
}
