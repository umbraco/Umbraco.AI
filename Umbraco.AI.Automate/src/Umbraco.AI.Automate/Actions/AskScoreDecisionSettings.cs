using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="AskScoreDecisionAction"/>.
/// </summary>
public sealed class AskScoreDecisionSettings
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
    /// Gets or sets the score's labels, lowest first. Requires 2 to 10 non-blank entries
    /// (enforced by Core's Decision validator).
    /// </summary>
    [Field(Label = "Levels",
        Description = "The score's labels, lowest first. Requires 2 to 10 entries.",
        SortOrder = 2,
        EditorUiAlias = "Umb.PropertyEditorUi.MultipleTextString")]
    public IReadOnlyList<string> Levels { get; set; } = [];

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
