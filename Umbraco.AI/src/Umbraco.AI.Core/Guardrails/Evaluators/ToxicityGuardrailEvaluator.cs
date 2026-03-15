using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the toxicity guardrail evaluator.
/// </summary>
public class ToxicityGuardrailEvaluatorConfig
{
    /// <summary>
    /// Comma-separated list of blocked words or phrases.
    /// </summary>
    [AIField(
        Label = "Blocked Words",
        Description = "Comma-separated list of words or phrases to block",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 1)]
    public string? BlockedWords { get; set; }

    /// <summary>
    /// Comma-separated list of regex patterns to block.
    /// </summary>
    [AIField(
        Label = "Blocked Patterns",
        Description = "Comma-separated list of regex patterns to block",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        EditorConfig = "[{\"alias\":\"rows\",\"value\":3}]",
        SortOrder = 2)]
    public string? BlockedPatterns { get; set; }

    /// <summary>
    /// Whether matching should be case-insensitive.
    /// </summary>
    [AIField(
        Label = "Case Insensitive",
        Description = "Match words and patterns regardless of case",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 3)]
    public bool CaseInsensitive { get; set; } = true;
}

/// <summary>
/// Guardrail evaluator that detects toxic content using keyword and pattern matching.
/// </summary>
[AIGuardrailEvaluator("toxicity", "Toxicity Detection", Type = AIGuardrailEvaluatorType.CodeBased)]
public class ToxicityGuardrailEvaluator : AIGuardrailEvaluatorBase<ToxicityGuardrailEvaluatorConfig>
{
    /// <inheritdoc />
    public override string Description => "Detects toxic content using configurable blocked words and regex patterns";

    public ToxicityGuardrailEvaluator(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    { }

    /// <inheritdoc />
    public override Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<ToxicityGuardrailEvaluatorConfig>() ?? new ToxicityGuardrailEvaluatorConfig();
        var matches = new List<string>();
        var comparison = evalConfig.CaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // Check blocked words
        if (!string.IsNullOrWhiteSpace(evalConfig.BlockedWords))
        {
            var words = evalConfig.BlockedWords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var word in words)
            {
                if (content.Contains(word, comparison))
                {
                    matches.Add($"word: {word}");
                }
            }
        }

        // Check blocked patterns
        if (!string.IsNullOrWhiteSpace(evalConfig.BlockedPatterns))
        {
            var patterns = evalConfig.BlockedPatterns
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var regexOptions = evalConfig.CaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;

            foreach (var pattern in patterns)
            {
                try
                {
                    if (Regex.IsMatch(content, pattern, regexOptions, TimeSpan.FromSeconds(5)))
                    {
                        matches.Add($"pattern: {pattern}");
                    }
                }
                catch (RegexParseException)
                {
                    // Skip invalid patterns
                }
            }
        }

        var flagged = matches.Count > 0;
        return Task.FromResult(new AIGuardrailResult
        {
            EvaluatorId = Id,
            Flagged = flagged,
            Score = flagged ? 1.0 : 0.0,
            Reason = flagged ? $"Toxic content detected: {string.Join("; ", matches)}" : null,
            Metadata = flagged
                ? JsonSerializer.SerializeToElement(new { matches }, Constants.DefaultJsonSerializerOptions)
                : null
        });
    }
}
