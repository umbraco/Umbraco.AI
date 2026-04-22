using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Settings for the <see cref="AgentRunFailedTrigger"/>.
/// </summary>
public sealed class AgentRunFailedTriggerSettings
{
    /// <summary>
    /// Gets or sets the agent to filter on. When empty, the trigger fires for any agent.
    /// </summary>
    [Field(
        Label = "Agent",
        Description = "Only fire when this agent fails. Leave blank to fire for any agent.",
        EditorUiAlias = "Uai.PropertyEditorUi.AgentPicker")]
    public Guid AgentId { get; set; }
}
