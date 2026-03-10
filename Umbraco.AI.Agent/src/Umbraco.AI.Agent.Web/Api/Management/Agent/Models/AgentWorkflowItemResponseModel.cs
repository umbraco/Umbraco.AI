using Umbraco.AI.Web.Api.Management.Provider.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for an agent workflow.
/// </summary>
public class AgentWorkflowItemResponseModel
{
    /// <summary>
    /// The unique identifier for this workflow.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The display name for this workflow.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional description of what this workflow does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The settings schema for this workflow, or null if no settings are required.
    /// </summary>
    public EditableModelSchemaModel? SettingsSchema { get; set; }
}
