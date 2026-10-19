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
    /// Gets or sets the options to choose from. Each entry has a <see cref="AskChoiceDecisionOption.Key"/>
    /// (the machine value returned when the AI picks it) and an optional
    /// <see cref="AskChoiceDecisionOption.Value"/> description shown to the AI to help it choose.
    /// Requires 2 to 255 entries with unique keys (enforced by Core's Decision validator). Not
    /// bindable — Automate only resolves bindings on <c>string</c>/<c>IList&lt;string&gt;</c>
    /// settings, and this is a complex list.
    /// </summary>
    [Field(Label = "Options",
        Description = "2 to 255 options, each with a unique key. The value is an optional description shown to the AI.",
        SortOrder = 2,
        EditorUiAlias = "Uai.PropertyEditorUi.KeyValueList",
        EditorConfig = """[{ "alias": "min", "value": 2 }, { "alias": "max", "value": 255 }, { "alias": "keyLabel", "value": "Key" }, { "alias": "valueLabel", "value": "Description" }]""")]
    public List<AskChoiceDecisionOption> Options { get; set; } = [];

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
