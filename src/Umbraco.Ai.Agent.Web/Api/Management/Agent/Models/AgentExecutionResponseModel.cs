using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for agent execution.
/// </summary>
public class AgentExecutionResponseModel
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
