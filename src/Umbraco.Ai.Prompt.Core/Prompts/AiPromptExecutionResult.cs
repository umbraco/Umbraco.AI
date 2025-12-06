using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Result of prompt execution.
/// </summary>
public class AiPromptExecutionResult
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
