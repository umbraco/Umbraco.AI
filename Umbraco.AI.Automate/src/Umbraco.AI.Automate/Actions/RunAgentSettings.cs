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

    /// <summary>
    /// Gets or sets optional media to attach to the message (images, documents, audio).
    /// Accepts media keys, <c>umb://media/…</c> UDIs, or a media picker value, comma or
    /// newline separated. Supports binding syntax.
    /// </summary>
    [Field(Label = "Attachments",
        Description = "Optional media to send with the message: media keys, media UDIs, or a media picker value, comma or newline separated. Supports ${ binding } syntax.",
        SupportsBindings = true,
        SortOrder = 2)]
    public string? Attachments { get; set; }

    /// <summary>
    /// Gets or sets which tool calls the agent may make, as a <see cref="RunAgentToolPermissions"/> name.
    /// </summary>
    [Field(
        Label = "Tool permissions",
        Description = "Choose whether the agent can only look things up, or can also make changes "
            + "that don't need approval, like editing drafts. Changes that need approval, like "
            + "publishing or deleting, are always blocked.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown.Single",
        EditorConfig = """
            [{ "alias": "items", "value": [
                { "name": "Read only", "value": "ReadOnly" },
                { "name": "Changes that don't need approval", "value": "NoApprovalRequired" }
            ] }]
            """,
        SortOrder = 3)]
    public string ToolPermissions { get; set; } = nameof(RunAgentToolPermissions.ReadOnly);
}
