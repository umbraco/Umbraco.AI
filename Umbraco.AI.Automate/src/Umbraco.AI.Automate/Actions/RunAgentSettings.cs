using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="RunAgentAction"/>.
/// </summary>
public sealed class RunAgentSettings
{
    /// <summary>
    /// Gets or sets the ID of the AI agent to run.
    /// </summary>
    [Field(Label = "Agent", Description = "The AI agent to execute.",
        EditorUiAlias = "Uai.PropertyEditorUi.AgentPicker",
        EditorConfig = """[{ "alias": "surfaceId", "value": "automations" }]""")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// Gets or sets the message to send to the agent. Supports binding syntax.
    /// </summary>
    [Field(Label = "Message", Description = "The message to send to the AI agent. Supports ${ binding } syntax.", SupportsBindings = true, SortOrder = 1)]
    public string Message { get; set; } = string.Empty;
}
