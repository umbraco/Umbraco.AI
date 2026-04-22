using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Output produced when an AI agent run completes successfully.
/// </summary>
public sealed class AgentRunCompletedTriggerOutput
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
    /// Gets the agent's final response text. Empty for streaming executions.
    /// </summary>
    [Field(Label = "Response", Description = "The agent's final response text. Empty for streaming executions.")]
    public string Response { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total execution time in seconds.
    /// </summary>
    [Field(Label = "Duration (seconds)", Description = "Total execution time in seconds.")]
    public double DurationSeconds { get; init; }
}
