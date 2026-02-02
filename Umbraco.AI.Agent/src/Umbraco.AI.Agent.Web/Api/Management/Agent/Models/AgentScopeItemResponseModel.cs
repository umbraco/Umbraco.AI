namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for an agent scope.
/// </summary>
/// <remarks>
/// Name and description are localized on the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentScope_{id}Label</c></item>
///   <item>Description: <c>uaiAgentScope_{id}Description</c></item>
/// </list>
/// </remarks>
public class AgentScopeItemResponseModel
{
    /// <summary>
    /// The unique identifier for this scope.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The icon to display for this scope.
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
