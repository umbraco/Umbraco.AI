using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// Provider-declared, profile-level chat settings for OpenAI (surfaced on the profile editor and
/// applied to each request).
/// </summary>
public class OpenAIChatCapabilitySettings
{
    /// <summary>
    /// Constrains the reasoning effort for reasoning-capable models (the o-series and the GPT-5 line).
    /// Leave empty for the model default.
    /// </summary>
    /// <remarks>
    /// The levels offered are the ones the pinned OpenAI SDK can express through
    /// <c>ResponseReasoningEffortLevel</c>. The API also accepts <c>xhigh</c> and <c>max</c> on some
    /// models; those need an SDK that exposes them and are deliberately not offered here rather than
    /// silently dropped.
    /// </remarks>
    [AIField(
        Label = "Reasoning effort",
        Description = "Constrains reasoning effort for reasoning-capable models (the o-series and the GPT-5 line). Leave empty for the model default.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"none\",\"minimal\",\"low\",\"medium\",\"high\"]}]",
        SortOrder = 1)]
    public string? ReasoningEffort { get; set; }
}
