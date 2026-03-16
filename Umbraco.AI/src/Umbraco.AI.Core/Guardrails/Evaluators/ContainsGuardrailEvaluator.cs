using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the contains guardrail evaluator.
/// </summary>
public class ContainsGuardrailEvaluatorConfig
{
    /// <summary>
    /// The substring to search for in the content.
    /// </summary>
    [AIField(
        Label = "Search Pattern",
        Description = "The substring to find in the content",
        EditorUiAlias = "Umb.PropertyEditorUi.TextArea",
        SortOrder = 1)]
    public string SearchPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ignore case when searching.
    /// </summary>
    [AIField(
        Label = "Ignore Case",
        Description = "Case-insensitive search",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool IgnoreCase { get; set; } = true;
}

/// <summary>
/// Guardrail evaluator that flags content containing a specific substring.
/// </summary>
[AIGuardrailEvaluator("contains", "Contains", Type = AIGuardrailEvaluatorType.CodeBased)]
public class ContainsGuardrailEvaluator : AIGuardrailEvaluatorBase<ContainsGuardrailEvaluatorConfig>, IAIRedactableGuardrail
{
    /// <inheritdoc />
    public override string Description => "Flags content that contains a specific substring";

    public ContainsGuardrailEvaluator(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    { }

    /// <inheritdoc />
    public override Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<ContainsGuardrailEvaluatorConfig>() ?? new ContainsGuardrailEvaluatorConfig();

        var comparison = evalConfig.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var flagged = !string.IsNullOrEmpty(evalConfig.SearchPattern)
            && content.Contains(evalConfig.SearchPattern, comparison);

        return Task.FromResult(new AIGuardrailResult
        {
            EvaluatorId = Id,
            Flagged = flagged,
            Score = flagged ? 1.0 : 0.0,
            Reason = flagged ? $"Content contains '{evalConfig.SearchPattern}'" : null,
            Metadata = flagged
                ? JsonSerializer.SerializeToElement(new { searchPattern = evalConfig.SearchPattern }, Constants.DefaultJsonSerializerOptions)
                : null
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AIGuardrailRedactableMatch>> FindRedactableMatchesAsync(
        string content,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<ContainsGuardrailEvaluatorConfig>() ?? new ContainsGuardrailEvaluatorConfig();

        if (string.IsNullOrEmpty(evalConfig.SearchPattern))
        {
            return Task.FromResult<IReadOnlyList<AIGuardrailRedactableMatch>>([]);
        }

        var comparison = evalConfig.IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var matches = new List<AIGuardrailRedactableMatch>();
        var startIndex = 0;

        while (startIndex < content.Length)
        {
            var index = content.IndexOf(evalConfig.SearchPattern, startIndex, comparison);
            if (index < 0)
            {
                break;
            }

            matches.Add(new AIGuardrailRedactableMatch(index, evalConfig.SearchPattern.Length, content.Substring(index, evalConfig.SearchPattern.Length)));
            startIndex = index + evalConfig.SearchPattern.Length;
        }

        return Task.FromResult<IReadOnlyList<AIGuardrailRedactableMatch>>(matches);
    }
}
