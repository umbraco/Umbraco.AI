using System.Text.Json.Serialization;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Provider-declared, profile-level image-generation settings for OpenAI (surfaced on the profile editor
/// and applied to each request).
/// </summary>
/// <remarks>
/// Quality and style live here rather than in the core profile settings because Microsoft.Extensions.AI
/// models neither as a first-class option — they exist only as provider hints, and their accepted values
/// differ per model. Size and media type stay core, where M.E.AI does model them.
/// </remarks>
public class OpenAIImageCapabilitySettings
{
    /// <summary>
    /// The rendering quality. Leave empty for the model default.
    /// </summary>
    /// <remarks>
    /// One field covering two vocabularies: DALL·E 3 accepts <c>standard</c> and <c>hd</c>, gpt-image
    /// accepts <c>low</c>, <c>medium</c>, <c>high</c> and <c>auto</c>. A value the selected model's family
    /// does not accept is dropped rather than sent, so a profile carried across models degrades instead of
    /// failing. Per-model option lists would need a declaration channel the metadata does not have yet.
    /// </remarks>
    [AIField(
        Label = "Quality",
        Description = "Rendering quality. DALL·E 3 accepts standard and hd; gpt-image accepts low, medium, high and auto. Leave empty for the model default.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"auto\",\"low\",\"medium\",\"high\",\"standard\",\"hd\"]}]",
        SortOrder = 1)]
    [JsonConverter(typeof(DropdownStringJsonConverter))]
    public string? Quality { get; set; }

    /// <summary>
    /// The rendering style. DALL·E 3 only — declared unsupported on every other model, so the field does
    /// not render for them.
    /// </summary>
    [AIField(
        Label = "Style",
        Description = "Rendering style, supported by DALL·E 3 only. Leave empty for the model default.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"vivid\",\"natural\"]}]",
        SortOrder = 2)]
    [JsonConverter(typeof(DropdownStringJsonConverter))]
    public string? Style { get; set; }
}
