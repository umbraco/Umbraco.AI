using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="AskChoiceDecisionAction"/>.
/// </summary>
public sealed class AskChoiceDecisionSettings
{
    /// <summary>
    /// Gets or sets what to decide. Supports binding syntax so the value can be produced by an
    /// upstream action.
    /// </summary>
    [Field(Label = "Instructions",
        Description = "What to decide. Supports binding syntax.",
        SupportsBindings = true)]
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content to judge against <see cref="Instructions"/>, when there is any.
    /// Supports binding syntax so the value can be produced by an upstream action.
    /// </summary>
    [Field(Label = "Context",
        Description = "Optional content to judge against the instructions. Supports binding syntax.",
        SortOrder = 1,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SupportsBindings = true)]
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the options to choose from, one per line. No existing Automate field editor
    /// pairs a key with an optional description, so each line is either just <c>key</c> or
    /// <c>key: description</c> — parsed deterministically by <see cref="AskChoiceDecisionAction"/>,
    /// splitting on the first <c>:</c> only, so a key must not itself contain <c>:</c>.
    /// Requires 2 to 255 non-blank lines with unique keys (enforced by Core's Decision validator).
    /// </summary>
    [Field(Label = "Options",
        Description = "One option per line, as 'key' or 'key: description' (key must not contain ':'). Requires 2 to 255 lines with unique keys.",
        SortOrder = 2,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea")]
    public string Options { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the Decision profile to use. When empty, the default Decision
    /// profile is used.
    /// </summary>
    [Field(Label = "Profile",
        Description = "The Decision profile to use. Leave empty to use the default profile.",
        SortOrder = 3,
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = """[{ "alias": "capability", "value": "Decision" }]""")]
    public Guid? ProfileId { get; set; }
}
