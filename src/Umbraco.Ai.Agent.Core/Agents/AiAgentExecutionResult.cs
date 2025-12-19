using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Result of prompt execution.
/// </summary>
public class AiAgentExecutionResult
{
    /// <summary>
    /// The generated response content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }
}
