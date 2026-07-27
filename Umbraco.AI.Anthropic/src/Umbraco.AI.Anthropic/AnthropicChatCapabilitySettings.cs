using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// Provider-declared, profile-level chat settings for Anthropic (surfaced on the profile editor and
/// applied to each request).
/// </summary>
public class AnthropicChatCapabilitySettings
{
    /// <summary>
    /// Token budget for Claude's extended thinking on supported models. Leave empty to use the
    /// model default (extended thinking off). Anthropic requires a budget of at least 1024 tokens.
    /// </summary>
    [AIField(
        Label = "Thinking budget (tokens)",
        Description = "Token budget for Claude's extended thinking on supported models. Leave empty to disable. Must be at least 1024 when set.",
        SortOrder = 1)]
    [Range(1024, int.MaxValue)]
    public int? ThinkingBudgetTokens { get; set; }
}
