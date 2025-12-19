using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Web.Api.Management.Prompt.Models;

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
}
