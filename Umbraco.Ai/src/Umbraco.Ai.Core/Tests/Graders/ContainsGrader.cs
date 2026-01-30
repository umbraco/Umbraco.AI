using System.Text.Json;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Code-based grader that validates output contains expected text.
/// Fast and deterministic for verifying presence of specific content.
/// </summary>
[AiTestGrader("contains", "Contains")]
public class ContainsGrader : IAiTestGrader
{
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainsGrader"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating UI configuration.</param>
    public ContainsGrader(IAiEditableModelSchemaBuilder schemaBuilder)
    {
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public string Id => "contains";

    /// <inheritdoc />
    public string Name => "Contains";

    /// <inheritdoc />
    public string Description => "Validates that the output contains the expected text";

    /// <inheritdoc />
    public GraderType Type => GraderType.CodeBased;

    /// <inheritdoc />
    public Type? ConfigType => typeof(ContainsGraderConfig);

    /// <inheritdoc />
    public AiEditableModelSchema? GetConfigSchema()
    {
        return _schemaBuilder.BuildForType<ContainsGraderConfig>(Id);
    }

    /// <inheritdoc />
    public Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<ContainsGraderConfig>(graderConfig.ConfigJson)
            ?? throw new InvalidOperationException("Failed to deserialize ContainsGraderConfig");

        var actualValue = outcome.OutputValue;
        var expectedText = config.Text;

        // Perform search
        var passed = config.CaseSensitive
            ? actualValue.Contains(expectedText, StringComparison.Ordinal)
            : actualValue.Contains(expectedText, StringComparison.OrdinalIgnoreCase);

        var result = new AiTestGraderResult
        {
            GraderId = graderConfig.Id,
            Passed = passed,
            Score = null, // Binary grader - no score
            ActualValue = actualValue,
            ExpectedValue = expectedText,
            FailureMessage = passed ? null : $"Expected output to contain '{expectedText}', but it was not found"
        };

        return Task.FromResult(result);
    }
}
