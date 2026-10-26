using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// Settings for the <see cref="ExtractValuesAction"/>.
/// </summary>
public sealed class ExtractValuesSettings
{
    /// <summary>
    /// Gets or sets the fields to extract in one call, 1 to 20 entries. Not bindable: Automate only
    /// resolves bindings on <c>string</c>/<c>IList&lt;string&gt;</c> settings, and this is a complex list.
    /// </summary>
    [Field(Label = "Fields",
        Description = "1 to 20 fields, each extracted in a single call. Add a field, click a row to edit it.",
        EditorUiAlias = "Uai.PropertyEditorUi.ExtractionFieldList",
        EditorConfig = """[{ "alias": "max", "value": 20 }]""")]
    public List<ExtractValuesField> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the text to extract values from. Supports binding syntax so the value can be
    /// produced by an upstream step.
    /// </summary>
    [Field(Label = "Content",
        Description = "The text to extract values from. Supports binding syntax.",
        SortOrder = 1,
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SupportsBindings = true)]
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the date that relative dates in the content ("tomorrow") are resolved against,
    /// as an ISO date (<c>yyyy-MM-dd</c>) or ISO date-time. When empty, today's date (UTC) is used.
    /// </summary>
    [Field(Label = "Reference date",
        Description = "Resolves relative dates like 'tomorrow'. An ISO date (2026-10-20) or date-time. Leave empty to use today (UTC).",
        SortOrder = 2,
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SupportsBindings = true)]
    public string? ReferenceDate { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Extraction profile to use. When empty, the default Extraction
    /// profile is used.
    /// </summary>
    [Field(Label = "Profile",
        Description = "The Extraction profile to use. Leave empty to use the default profile.",
        SortOrder = 3,
        EditorUiAlias = "Uai.PropertyEditorUi.ProfilePicker",
        EditorConfig = """[{ "alias": "capability", "value": "Extraction" }]""")]
    public Guid? ProfileId { get; set; }
}
