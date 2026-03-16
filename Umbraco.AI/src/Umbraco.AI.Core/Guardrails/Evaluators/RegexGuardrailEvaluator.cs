using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the regex guardrail evaluator.
/// </summary>
public class RegexGuardrailEvaluatorConfig
{
    /// <summary>
    /// The regular expression pattern to match.
    /// </summary>
    [AIField(
        Label = "Regex Pattern",
        Description = "Regular expression pattern to match against content",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when matching.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive matching",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Whether to use multiline mode.
    /// </summary>
    [AIField(
        Label = "Multiline",
        Description = "Enable multiline mode (^ and $ match line boundaries)",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 3)]
    public bool Multiline { get; set; }
}

/// <summary>
/// Guardrail evaluator that flags content matching a regular expression pattern.
/// </summary>
[AIGuardrailEvaluator("regex", "Regex Match", Type = AIGuardrailEvaluatorType.CodeBased)]
public class RegexGuardrailEvaluator : AIGuardrailEvaluatorBase<RegexGuardrailEvaluatorConfig>, IAIRedactableGuardrailEvaluator
{
    /// <inheritdoc />
    public override string Description => "Flags content that matches a regular expression pattern";

    public RegexGuardrailEvaluator(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    { }

    /// <inheritdoc />
    public override Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<RegexGuardrailEvaluatorConfig>() ?? new RegexGuardrailEvaluatorConfig();

        if (string.IsNullOrEmpty(evalConfig.Pattern))
        {
            return Task.FromResult(new AIGuardrailResult
            {
                EvaluatorId = Id,
                Flagged = false,
                Score = 0.0
            });
        }

        var options = RegexOptions.None;
        if (evalConfig.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (evalConfig.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        bool flagged;
        string? reason = null;
        JsonElement? metadata = null;

        try
        {
            var regex = new Regex(evalConfig.Pattern, options, TimeSpan.FromSeconds(5));
            var match = regex.Match(content);
            flagged = match.Success;

            if (flagged)
            {
                reason = $"Content matches pattern: {evalConfig.Pattern}";
                metadata = JsonSerializer.SerializeToElement(new
                {
                    pattern = evalConfig.Pattern,
                    matchValue = match.Value,
                    matchIndex = match.Index,
                    matchLength = match.Length,
                    groups = match.Groups.Cast<Group>()
                        .Skip(1)
                        .Select(g => new { name = g.Name, value = g.Value })
                        .ToArray()
                }, Constants.DefaultJsonSerializerOptions);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            flagged = true;
            reason = "Regex matching timed out after 5 seconds";
        }
        catch (RegexParseException ex)
        {
            flagged = false;
            reason = $"Invalid regex pattern: {ex.Message}";
        }

        return Task.FromResult(new AIGuardrailResult
        {
            EvaluatorId = Id,
            Flagged = flagged,
            Score = flagged ? 1.0 : 0.0,
            Reason = reason,
            Metadata = metadata
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIGuardrailRedactionCandidate>> FindRedactionCandidatesAsync(
        string content,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<RegexGuardrailEvaluatorConfig>() ?? new RegexGuardrailEvaluatorConfig();

        if (string.IsNullOrEmpty(evalConfig.Pattern))
        {
            return Task.FromResult<IReadOnlyList<AIGuardrailRedactionCandidate>>([]);
        }

        var options = RegexOptions.None;
        if (evalConfig.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (evalConfig.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        try
        {
            var regex = new Regex(evalConfig.Pattern, options, TimeSpan.FromSeconds(5));
            var matches = regex.Matches(content);

            var results = matches
                .Select(m => new AIGuardrailRedactionCandidate(m.Index, m.Length, m.Value))
                .ToList();

            return Task.FromResult<IReadOnlyList<AIGuardrailRedactionCandidate>>(results);
        }
        catch (RegexMatchTimeoutException)
        {
            return Task.FromResult<IReadOnlyList<AIGuardrailRedactionCandidate>>([]);
        }
        catch (RegexParseException)
        {
            return Task.FromResult<IReadOnlyList<AIGuardrailRedactionCandidate>>([]);
        }
    }
}
