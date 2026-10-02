using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="AskYesNoDecisionAction"/>.
/// </summary>
public sealed class AskYesNoDecisionSettings
{
    /// <summary>
    /// Gets or sets what to decide, as a yes/no question. Supports binding syntax so the value
    /// can be produced by an upstream action.
    /// </summary>
    [Field(Label = "Instructions",
        Description = "What to decide, as a yes/no question. Supports binding syntax.",
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
    /// Gets or sets what "yes" means, when it needs spelling out beyond <see cref="Instructions"/>.
    /// Supports binding syntax so the value can be produced by an upstream action.
    /// </summary>
    [Field(Label = "True criteria",
        Description = "Optional elaboration of what counts as \"yes\", beyond the instructions. Supports binding syntax.",
        SortOrder = 2,
        SupportsBindings = true)]
    public string? TrueCriteria { get; set; }

    /// <summary>
    /// Gets or sets what "no" means, when it needs spelling out beyond <see cref="Instructions"/>.
    /// Supports binding syntax so the value can be produced by an upstream action.
    /// </summary>
    [Field(Label = "False criteria",
        Description = "Optional elaboration of what counts as \"no\", beyond the instructions. Supports binding syntax.",
        SortOrder = 3,
        SupportsBindings = true)]
    public string? FalseCriteria { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Decision profile to use. When empty, the default Decision
    /// profile is used.
    /// </summary>
    [Field(Label = "Profile",
        Description = "The Decision profile to use. Leave empty to use the default profile.",
        SortOrder = 4,
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = """[{ "alias": "capability", "value": "Decision" }]""")]
    public Guid? ProfileId { get; set; }
}
