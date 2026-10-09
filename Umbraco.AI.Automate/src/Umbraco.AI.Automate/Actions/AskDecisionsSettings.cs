using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="AskDecisionsAction"/>.
/// </summary>
public sealed class AskDecisionsSettings
{
    /// <summary>
    /// Gets or sets the questions to ask in one call, 1 to 20 entries, each configured through
    /// the picker + per-kind config modal. Not bindable — Automate only resolves bindings on
    /// <c>string</c>/<c>IList&lt;string&gt;</c> settings, and this is a complex list.
    /// </summary>
    [Field(Label = "Questions",
        Description = "1 to 20 questions, each asked in a single call. Add through the picker, click a row to edit it.",
        EditorUiAlias = "Uai.PropertyEditorUi.DecisionQuestionList",
        EditorConfig = """[{ "alias": "max", "value": 20 }]""")]
    public List<AskDecisionsQuestion> Questions { get; set; } = [];

    /// <summary>
    /// Gets or sets the content to judge against every question's instructions, when there is
    /// any. Supports binding syntax so the value can be produced by an upstream action.
    /// </summary>
    [Field(Label = "Context",
        Description = "Optional content to judge against every question's instructions. Supports binding syntax.",
        SortOrder = 1,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SupportsBindings = true)]
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Decision profile to use. When empty, the default Decision
    /// profile is used.
    /// </summary>
    [Field(Label = "Profile",
        Description = "The Decision profile to use. Leave empty to use the default profile.",
        SortOrder = 2,
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = """[{ "alias": "capability", "value": "Decision" }]""")]
    public Guid? ProfileId { get; set; }
}
