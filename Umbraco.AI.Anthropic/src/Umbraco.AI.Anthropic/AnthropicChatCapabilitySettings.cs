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

    /// <summary>
    /// How long Anthropic keeps the reusable part of this profile's prompt in its cache. Leave empty to
    /// send every request uncached.
    /// </summary>
    /// <remarks>
    /// Maps to the request's top-level <c>cache_control</c>, which marks the last cacheable block and moves
    /// that mark forward as a conversation grows — Anthropic's recommended shape for multi-turn use, and
    /// the only one reachable here (see <c>AnthropicChatCapability.ApplyCapabilitySettings</c>).
    /// <para>
    /// Both offered values are the complete set Anthropic accepts. Off is the default because caching only
    /// pays off when the prefix is genuinely reused: a write costs more than a plain request (1.25× base
    /// input at 5m, 2× at 1h) and only a read is cheaper (0.1×). <c>5m</c> suits anything invoked
    /// repeatedly, since a hit refreshes the entry for free; <c>1h</c> earns its higher write cost only for
    /// a profile used less often than every five minutes.
    /// </para>
    /// <para>
    /// Anthropic silently declines to cache a prefix below a model-dependent minimum (roughly 512–4,096
    /// tokens), so enabling this on a short prompt is harmless but has no effect.
    /// </para>
    /// </remarks>
    [AIField(
        Label = "Prompt caching",
        Description = "Reuse the stable start of this profile's prompt across requests, billed at a discount. Leave empty to disable. Has no effect on prompts below the model's minimum cacheable length.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"5m\",\"1h\"]}]",
        SortOrder = 2)]
    [JsonConverter(typeof(DropdownStringJsonConverter))]
    public string? PromptCaching { get; set; }
}
