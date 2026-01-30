using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Embeddings;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Model-based grader that validates semantic similarity using embeddings.
/// Uses cosine similarity to compare the output with expected text semantically,
/// allowing for paraphrased or rephrased responses.
/// </summary>
[AiTestGrader("semantic-similarity", "Semantic Similarity")]
public class SemanticSimilarityGrader : IAiTestGrader
{
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;
    private readonly IAiEmbeddingService _embeddingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSimilarityGrader"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating UI configuration.</param>
    /// <param name="embeddingService">The embedding service for generating embeddings.</param>
    public SemanticSimilarityGrader(
        IAiEditableModelSchemaBuilder schemaBuilder,
        IAiEmbeddingService embeddingService)
    {
        _schemaBuilder = schemaBuilder;
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public string Id => "semantic-similarity";

    /// <inheritdoc />
    public string Name => "Semantic Similarity";

    /// <inheritdoc />
    public string Description => "Validates semantic similarity using embeddings and cosine similarity";

    /// <inheritdoc />
    public GraderType Type => GraderType.ModelBased;

    /// <inheritdoc />
    public Type? ConfigType => typeof(SemanticSimilarityGraderConfig);

    /// <inheritdoc />
    public AiEditableModelSchema? GetConfigSchema()
    {
        return _schemaBuilder.BuildForType<SemanticSimilarityGraderConfig>(Id);
    }

    /// <inheritdoc />
    public async Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<SemanticSimilarityGraderConfig>(graderConfig.ConfigJson)
            ?? throw new InvalidOperationException("Failed to deserialize SemanticSimilarityGraderConfig");

        var actualValue = outcome.OutputValue;
        var expectedValue = config.ExpectedText;

        try
        {
            // Generate embeddings for both texts
            var actualEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                config.ProfileId,
                actualValue,
                null,
                cancellationToken);

            var expectedEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                config.ProfileId,
                expectedValue,
                null,
                cancellationToken);

            // Calculate cosine similarity
            var similarity = CalculateCosineSimilarity(
                actualEmbedding.Vector.ToArray(),
                expectedEmbedding.Vector.ToArray());

            var passed = similarity >= config.Threshold;

            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = similarity, // Similarity score 0-1
                ActualValue = actualValue,
                ExpectedValue = expectedValue,
                FailureMessage = passed
                    ? null
                    : $"Similarity score {similarity:F3} is below threshold {config.Threshold:F3}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    similarity = similarity,
                    threshold = config.Threshold
                })
            };

            return result;
        }
        catch (Exception ex)
        {
            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = 0,
                ActualValue = actualValue,
                ExpectedValue = expectedValue,
                FailureMessage = $"Failed to calculate semantic similarity: {ex.Message}"
            };

            return result;
        }
    }

    /// <summary>
    /// Calculates the cosine similarity between two vectors.
    /// </summary>
    /// <param name="vectorA">First vector.</param>
    /// <param name="vectorB">Second vector.</param>
    /// <returns>Cosine similarity score (0-1).</returns>
    private static float CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = MathF.Sqrt(magnitudeA);
        magnitudeB = MathF.Sqrt(magnitudeB);

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct / (magnitudeA * magnitudeB);
    }
}
