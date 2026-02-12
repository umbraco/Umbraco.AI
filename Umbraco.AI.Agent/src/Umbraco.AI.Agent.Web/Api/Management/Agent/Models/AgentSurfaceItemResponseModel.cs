namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for an agent surface.
/// </summary>
/// <remarks>
/// Name and description are localized on the frontend using the convention:
/// <list type="bullet">
///   <item>Name: <c>uaiAgentSurface_{id}Label</c></item>
///   <item>Description: <c>uaiAgentSurface_{id}Description</c></item>
/// </list>
/// </remarks>
public class AgentSurfaceItemResponseModel
{
    /// <summary>
    /// The unique identifier for this surface.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The icon to display for this surface.
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
