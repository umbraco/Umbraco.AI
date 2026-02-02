using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EntityAdapter;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Result of prompt execution.
/// </summary>
public class AIPromptExecutionResult
{
    /// <summary>
    /// The generated response content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Token usage information from Microsoft.Extensions.AI.
    /// </summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>
    /// Optional property changes to be applied to the entity.
    /// Returned when the prompt generates structured output for property updates.
    /// </summary>
    public IReadOnlyList<AIPropertyChange>? PropertyChanges { get; init; }
}
