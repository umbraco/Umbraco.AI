using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Response model for prompt execution.
/// </summary>
public class PromptExecutionResponseModel
{
    /// <summary>
    /// The generated response content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Token usage information.
    /// </summary>
    public UsageModel? Usage { get; init; }

    /// <summary>
    /// Optional property changes to be applied to the entity.
    /// Returned when the prompt generates structured output for property updates.
    /// </summary>
    public IReadOnlyList<PropertyChangeModel>? PropertyChanges { get; init; }
}
