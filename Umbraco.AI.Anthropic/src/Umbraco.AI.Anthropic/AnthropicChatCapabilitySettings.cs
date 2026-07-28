using System.Text.Json.Serialization;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Provider-declared, profile-level chat settings for Anthropic (surfaced on the profile editor and
/// applied to each request).
/// </summary>
public class AnthropicChatCapabilitySettings
{
    /// <summary>
    /// How many tokens Claude spends on a response, including thinking and tool calls. Leave empty for
    /// the model default (high).
    /// </summary>
    /// <remarks>
    /// Maps to <c>output_config.effort</c>, supported on Claude Opus 4.5 and everything from the 4.6
    /// generation onwards.
    /// <para>
    /// Only the three levels every effort-capable model accepts are offered. Anthropic's <c>xhigh</c> and
    /// <c>max</c> levels reach a subset of models that cannot be tracked accurately with a hard-coded
    /// list — the set with <c>xhigh</c> grows with each release, so an allow-list goes stale silently and a
    /// deny-list risks sending a level that is rejected. They belong with a declaration read from the
    /// models endpoint's per-model <c>capabilities.effort</c>, which reports the levels directly.
    /// </para>
    /// </remarks>
    [AIField(
        Label = "Effort",
        Description = "How many tokens Claude spends on a response, including thinking and tool calls. Leave empty for the model default (high).",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"low\",\"medium\",\"high\"]}]",
        SortOrder = 1)]
    [JsonConverter(typeof(DropdownStringJsonConverter))]
    public string? Effort { get; set; }
}
