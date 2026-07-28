using Umbraco.AI.Core.EditableModels;

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
    /// Maps to <c>output_config.effort</c>. Supported on Claude Opus 4.5 and everything from the 4.6
    /// generation onwards; the <c>xhigh</c> and <c>max</c> levels are available on fewer models than the
    /// rest, and a level the selected model does not accept is dropped rather than sent.
    /// </remarks>
    [AIField(
        Label = "Effort",
        Description = "How many tokens Claude spends on a response, including thinking and tool calls. Leave empty for the model default (high). The xhigh and max levels are only available on newer models.",
        EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
        EditorConfig = "[{\"alias\":\"multiple\",\"value\":false},{\"alias\":\"items\",\"value\":[\"low\",\"medium\",\"high\",\"xhigh\",\"max\"]}]",
        SortOrder = 1)]
    public string? Effort { get; set; }
}
