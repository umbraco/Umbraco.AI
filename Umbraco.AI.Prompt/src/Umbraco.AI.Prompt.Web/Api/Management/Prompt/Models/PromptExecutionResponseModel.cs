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
    /// Available result options. Always present, may be empty.
    /// - Empty array: Informational only
    /// - Single item: One value to insert
    /// - Multiple items: User selects one
    /// </summary>
    public required IReadOnlyList<ResultOptionModel> ResultOptions { get; init; }
}
