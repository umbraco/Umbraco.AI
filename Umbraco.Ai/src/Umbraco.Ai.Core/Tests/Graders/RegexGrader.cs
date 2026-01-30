using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Tests.Graders;

/// <summary>
/// Code-based grader that validates output matches a regular expression pattern.
/// Fast and deterministic for verifying formatted content (emails, phone numbers, etc.).
/// </summary>
[AiTestGrader("regex", "Regular Expression")]
public class RegexGrader : IAiTestGrader
{
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexGrader"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder for generating UI configuration.</param>
    public RegexGrader(IAiEditableModelSchemaBuilder schemaBuilder)
    {
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public string Id => "regex";

    /// <inheritdoc />
    public string Name => "Regular Expression";

    /// <inheritdoc />
    public string Description => "Validates that the output matches a regular expression pattern";

    /// <inheritdoc />
    public GraderType Type => GraderType.CodeBased;

    /// <inheritdoc />
    public Type? ConfigType => typeof(RegexGraderConfig);

    /// <inheritdoc />
    public AiEditableModelSchema? GetConfigSchema()
    {
        return _schemaBuilder.BuildForType<RegexGraderConfig>(Id);
    }

    /// <inheritdoc />
    public Task<AiTestGraderResult> GradeAsync(
        AiTestTranscript transcript,
        AiTestOutcome outcome,
        AiTestGrader graderConfig,
        CancellationToken cancellationToken = default)
    {
        var config = JsonSerializer.Deserialize<RegexGraderConfig>(graderConfig.ConfigJson)
            ?? throw new InvalidOperationException("Failed to deserialize RegexGraderConfig");

        var actualValue = outcome.OutputValue;

        try
        {
            // Build regex options
            var options = RegexOptions.None;
            if (!config.CaseSensitive)
                options |= RegexOptions.IgnoreCase;
            if (config.Multiline)
                options |= RegexOptions.Multiline;

            // Perform regex match
            var regex = new Regex(config.Pattern, options);
            var match = regex.Match(actualValue);
            var passed = match.Success;

            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = passed,
                Score = null, // Binary grader - no score
                ActualValue = actualValue,
                ExpectedValue = config.Pattern,
                FailureMessage = passed
                    ? null
                    : $"Output did not match pattern '{config.Pattern}'"
            };

            return Task.FromResult(result);
        }
        catch (ArgumentException ex)
        {
            // Invalid regex pattern
            var result = new AiTestGraderResult
            {
                GraderId = graderConfig.Id,
                Passed = false,
                Score = null,
                ActualValue = actualValue,
                ExpectedValue = config.Pattern,
                FailureMessage = $"Invalid regex pattern: {ex.Message}"
            };

            return Task.FromResult(result);
        }
    }
}
