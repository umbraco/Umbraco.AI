using System.Text.Json;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Code-based grader that validates output exactly matches expected text.
/// Fast and deterministic for verifying specific responses.
/// </summary>
[AiTestGrader("exact-match", "Exact Match")]
public class ExactMatchGrader : IAiTestGrader
{
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExactMatchGrader"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating UI configuration.</param>
    public ExactMatchGrader(IAiEditableModelSchemaBuilder schemaBuilder)
    {
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public string Id => "exact-match";

    /// <inheritdoc />
    public string Name => "Exact Match";

    /// <inheritdoc />
    public string Description => "Validates that the output exactly matches the expected text";

    /// <inheritdoc />
    public GraderType Type => GraderType.CodeBased;

    /// <inheritdoc />
    public Type? ConfigType => typeof(ExactMatchGraderConfig);

    /// <inheritdoc />
    public AiEditableModelSchema? GetConfigSchema()
    {
        return _schemaBuilder.BuildForType<ExactMatchGraderConfig>(Id);
    }

    /// <inheritdoc />
    public Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<ExactMatchGraderConfig>(graderConfig.ConfigJson)
            ?? throw new InvalidOperationException("Failed to deserialize ExactMatchGraderConfig");

        var actualValue = outcome.OutputValue;
        var expectedValue = config.Expected;

        // Apply transformations
        if (config.Trim)
        {
            actualValue = actualValue.Trim();
            expectedValue = expectedValue.Trim();
        }

        // Perform comparison
        var passed = config.CaseSensitive
            ? actualValue.Equals(expectedValue, StringComparison.Ordinal)
            : actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);

        var result = new AiTestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = null, // Binary grader - no score
            ActualValue = actualValue,
            ExpectedValue = expectedValue,
            FailureMessage = passed ? null : $"Expected exact match for '{expectedValue}', but got '{actualValue}'"
        };

        return Task.FromResult(result);
    }
}
