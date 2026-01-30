using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Configuration for the semantic similarity grader.
/// </summary>
public class SemanticSimilarityGraderConfig
{
    /// <summary>
    /// The embedding profile ID to use for similarity calculation.
    /// </summary>
    [AiField(
        Label = "Embedding Profile ID",
        Description = "The AI profile ID to use for generating embeddings (must be an embedding profile)",
        SortOrder = 1)]
    public Guid ProfileId { get; set; }

    /// <summary>
    /// The reference text to compare against.
    /// </summary>
    [AiField(
        Label = "Expected Text",
        Description = "The reference text to compare the output against for semantic similarity",
        SortOrder = 2)]
    public string ExpectedText { get; set; } = string.Empty;

    /// <summary>
    /// The minimum similarity score (0-1) required to pass.
    /// </summary>
    [AiField(
        Label = "Threshold",
        Description = "Minimum similarity score (0-1) required to pass. 0.8 is recommended for similar content.",
        SortOrder = 3)]
    public float Threshold { get; set; } = 0.8f;
}
