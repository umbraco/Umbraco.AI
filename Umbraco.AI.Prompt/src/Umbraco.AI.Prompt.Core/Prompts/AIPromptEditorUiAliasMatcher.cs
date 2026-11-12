namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Matches a property editor UI alias stored in a scope rule against the alias a data type uses now.
/// CMS 19 moved some data types onto new, split UI aliases, so a rule saved against the old alias
/// also matches the new one. A rule naming a new alias only matches that alias.
/// </summary>
internal static class AIPromptEditorUiAliasMatcher
{
    // Old UI alias -> the UI aliases CMS 19's V_19_0_0 data type migrations moved data types onto
    // (MigrateSingleMediaPickerDataTypes, MigrateSingleUrlPickerDataTypes, MigrateSingleDropDownDataTypes,
    // MigrateRangeSliderDataTypes, MigrateTypedLabelDataTypes).
    private static readonly IReadOnlyDictionary<string, string[]> _splitAliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Umb.PropertyEditorUi.MediaPicker"] = ["Umb.PropertyEditorUi.MediaPicker.Single"],
            ["Umb.PropertyEditorUi.MultiUrlPicker"] = ["Umb.PropertyEditorUi.UrlPicker.Single"],
            ["Umb.PropertyEditorUi.Dropdown"] = ["Umb.PropertyEditorUi.Dropdown.Single"],
            ["Umb.PropertyEditorUi.Slider"] = ["Umb.PropertyEditorUi.Slider.Range"],
            ["Umb.PropertyEditorUi.Label"] =
            [
                "Umb.PropertyEditorUi.Label.Integer",
                "Umb.PropertyEditorUi.Label.BigInt",
                "Umb.PropertyEditorUi.Label.Decimal",
                "Umb.PropertyEditorUi.Label.DateTime",
                "Umb.PropertyEditorUi.Label.Time",
                "Umb.PropertyEditorUi.Label.Text",
            ],
        };

    /// <summary>
    /// Returns true if any rule alias equals <paramref name="actualAlias"/>, or is an old alias
    /// that <paramref name="actualAlias"/> was split from.
    /// </summary>
    internal static bool Matches(IEnumerable<string> ruleAliases, string actualAlias)
    {
        foreach (var ruleAlias in ruleAliases)
        {
            if (string.Equals(ruleAlias, actualAlias, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (_splitAliases.TryGetValue(ruleAlias, out var splits)
                && splits.Contains(actualAlias, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
