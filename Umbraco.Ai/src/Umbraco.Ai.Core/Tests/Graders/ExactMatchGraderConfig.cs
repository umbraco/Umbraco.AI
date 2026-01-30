using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Configuration for the exact match grader.
/// </summary>
public class ExactMatchGraderConfig
{
    /// <summary>
    /// The text that must match exactly.
    /// </summary>
    [AiField(
        Label = "Expected Text",
        Description = "The text that must match exactly",
        SortOrder = 1)]
    public string Expected { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore leading/trailing whitespace.
    /// </summary>
    [AiField(
        Label = "Trim Whitespace",
        Description = "Ignore leading/trailing whitespace when comparing",
        SortOrder = 2)]
    public bool Trim { get; set; } = true;

    /// <summary>
    /// Whether the comparison should be case-sensitive.
    /// </summary>
    [AiField(
        Label = "Case Sensitive",
        Description = "Match case exactly",
        SortOrder = 3)]
    public bool CaseSensitive { get; set; } = true;
}
