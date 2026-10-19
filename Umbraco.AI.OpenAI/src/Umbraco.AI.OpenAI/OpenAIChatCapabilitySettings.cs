using System.Text.Json.Serialization;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Provider-declared, profile-level chat settings for OpenAI (surfaced on the profile editor and
/// applied to each request).
/// </summary>
public class OpenAIChatCapabilitySettings
{
    /// <summary>
    /// Constrains the reasoning effort for reasoning-capable models.
    /// Leave empty for the model default.
    /// </summary>
    /// <remarks>
    /// The schema is shared across models. Extended levels use the SDK's extensible string enum
    /// where supported and fall back to high otherwise. Legacy minimal values map to low on Luna.
    /// </remarks>
    [AIField(
        Label = "Reasoning effort",
        Description = "Constrains reasoning effort for reasoning-capable models. Leave empty for the model default. Extended levels fall back to high where unsupported; legacy minimal values may map to low.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"none\",\"minimal\",\"low\",\"medium\",\"high\",\"xhigh\",\"max\"]}]",
        SortOrder = 1)]
    [JsonConverter(typeof(DropdownStringJsonConverter))]
    public string? ReasoningEffort { get; set; }
}
