using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Provider-declared, profile-level chat settings for OpenAI (surfaced on the profile editor and
/// applied to each request).
/// </summary>
public class OpenAIChatProfileSettings
{
    /// <summary>
    /// Constrains the reasoning effort for reasoning-capable models (o-series, gpt-5). Leave empty
    /// for the model default. Ignored by non-reasoning models.
    /// </summary>
    [AIField(
        Label = "Reasoning effort",
        Description = "Constrains reasoning effort for reasoning-capable models (o-series, gpt-5). Leave empty for the model default.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"low\",\"medium\",\"high\"]}]",
        SortOrder = 1)]
    public string? ReasoningEffort { get; set; }
}
