using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Configuration for the regex grader.
/// </summary>
public class RegexGraderConfig
{
    /// <summary>
    /// The regular expression pattern to match against.
    /// </summary>
    [AiField(
        Label = "Pattern",
        Description = "The regular expression pattern to match (e.g., '^\\d{3}-\\d{4}$' for phone numbers)",
        SortOrder = 1)]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pattern matching should be case-sensitive.
    /// </summary>
    [AiField(
        Label = "Case Sensitive",
        Description = "Match case exactly in the pattern",
        SortOrder = 2)]
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Whether to match the pattern across multiple lines.
    /// </summary>
    [AiField(
        Label = "Multiline",
        Description = "Allow pattern to match across multiple lines (enables ^ and $ to match line boundaries)",
        SortOrder = 3)]
    public bool Multiline { get; set; } = false;
}
