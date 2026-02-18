using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Embeddings;

namespace Umbraco.AI.Core.Tests.Graders;

/// <summary>
/// Configuration for semantic similarity grader.
/// </summary>
public class SemanticSimilarityGraderConfig
{
    /// <summary>
    /// Expected content for similarity comparison.
    /// </summary>
    [AIField(
        Label = "Expected Content",
        Description = "The expected semantic content",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 1)]
    public string ExpectedContent { get; set; } = string.Empty;

    /// <summary>
    /// The profile ID to use for embeddings (optional, uses default if not specified).
    /// </summary>
    [AIField(
        Label = "Embedding Profile ID",
        Description = "AI profile to use for embeddings (leave empty for default)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 2)]
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Similarity threshold (0-1).
    /// </summary>
    [AIField(
        Label = "Similarity Threshold",
        Description = "Minimum cosine similarity to pass (0-1)",
        EditorUiAlias = "Umb.PropertyEditorUi.Slider",
        EditorConfig = "[{\"alias\":\"minValue\",\"value\":0},{\"alias\":\"maxValue\",\"value\":1},{\"alias\":\"step\",\"value\":0.05}]",
        SortOrder = 3)]
    public double SimilarityThreshold { get; set; } = 0.8;
}

/// <summary>
/// Grader that measures semantic similarity using embeddings.
/// Uses cosine similarity between expected and actual content embeddings.
/// </summary>
[AITestGrader("semantic-similarity", "Semantic Similarity", Type = AIGraderType.ModelBased)]
public class SemanticSimilarityGrader : AITestGraderBase
{
    private readonly IAIEmbeddingService _embeddingService;

    /// <inheritdoc />
    public override string Description => "Measures semantic similarity using embeddings";

    /// <inheritdoc />
    public override Type? ConfigType => typeof(SemanticSimilarityGraderConfig);

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSimilarityGrader"/> class.
    /// </summary>
    public SemanticSimilarityGrader(
        IAIEmbeddingService embeddingService,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public override async Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGrader graderConfig,
        CancellationToken cancellationToken)
    {
        // Deserialize configuration
        var config = string.IsNullOrWhiteSpace(graderConfig.ConfigJson)
            ? new SemanticSimilarityGraderConfig()
            : JsonSerializer.Deserialize<SemanticSimilarityGraderConfig>(graderConfig.ConfigJson)
                ?? new SemanticSimilarityGraderConfig();

        if (string.IsNullOrWhiteSpace(config.ExpectedContent))
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                FailureMessage = "Expected content is required for semantic similarity grading"
            };
        }

        // Extract actual value from final output
        var actualValue = ExtractContentFromOutput(outcome.OutputValue);

        if (string.IsNullOrWhiteSpace(actualValue))
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                ActualValue = actualValue,
                ExpectedValue = config.ExpectedContent,
                FailureMessage = "Actual content is empty"
            };
        }

        try
        {
            // Generate embeddings for both texts
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
                [config.ExpectedContent, actualValue],
                config.ProfileId,
                cancellationToken);

            if (embeddings.Count != 2)
            {
                return new AITestGraderResult
                {
                    GraderId = graderConfig.Id,
                    Passed = false,
                    Score = 0.0,
                    ActualValue = actualValue,
                    ExpectedValue = config.ExpectedContent,
                    FailureMessage = "Failed to generate embeddings for comparison"
                };
            }

            // Calculate cosine similarity
            var expectedEmbedding = embeddings[0].Vector.ToArray();
            var actualEmbedding = embeddings[1].Vector.ToArray();

            var similarity = CalculateCosineSimilarity(expectedEmbedding, actualEmbedding);
            var passed = similarity >= config.SimilarityThreshold;

            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = similarity,
                ActualValue = actualValue,
                ExpectedValue = config.ExpectedContent,
                FailureMessage = passed
                    ? null
                    : $"Similarity {similarity:F3} below threshold {config.SimilarityThreshold:F3}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    cosineSimilarity = similarity,
                    threshold = config.SimilarityThreshold,
                    embeddingDimensions = expectedEmbedding.Length
                })
            };
        }
        catch (Exception ex)
        {
            return new AITestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0.0,
                ActualValue = actualValue,
                ExpectedValue = config.ExpectedContent,
                FailureMessage = $"Semantic similarity calculation failed: {ex.Message}"
            };
        }
    }

    private static string ExtractContentFromOutput(string? outputJson)
    {
        if (string.IsNullOrWhiteSpace(outputJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(outputJson);
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // If parsing fails, return raw JSON
        }

        return outputJson;
    }

    private static double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (var i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
