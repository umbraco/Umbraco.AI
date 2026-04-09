using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Settings for the <see cref="AgentExecutedTrigger"/>.
/// </summary>
public sealed class AgentExecutedTriggerSettings
{
    /// <summary>
    /// Gets or sets the agent ID to filter on. If null, all agent executions match.
    /// </summary>
    [Field(Label = "Agent", Description = "Only fire for this agent. Leave blank to match all agents.",
        EditorUiAlias = "Uai.PropertyEditorUi.AgentPicker",
        EditorConfig = """[{ "alias": "surfaceId", "value": "automations" }]""")]
    public Guid? AgentId { get; set; }
}
