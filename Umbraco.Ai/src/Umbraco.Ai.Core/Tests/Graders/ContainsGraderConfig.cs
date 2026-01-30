using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Configuration for the contains grader.
/// </summary>
public class ContainsGraderConfig
{
    /// <summary>
    /// The substring to search for in the output.
    /// </summary>
    [AiField(
        Label = "Text to Find",
        Description = "The substring that must be present in the output",
        SortOrder = 1)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Whether the search should be case-sensitive.
    /// </summary>
    [AiField(
        Label = "Case Sensitive",
        Description = "Match case exactly when searching",
        SortOrder = 2)]
    public bool CaseSensitive { get; set; } = false;
}
