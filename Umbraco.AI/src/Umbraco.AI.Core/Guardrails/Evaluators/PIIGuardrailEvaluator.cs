using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Configuration for the PII guardrail evaluator.
/// </summary>
public class PIIGuardrailEvaluatorConfig
{
    /// <summary>
    /// Whether to detect email addresses.
    /// </summary>
    [AIField(
        Label = "Detect Emails",
        Description = "Flag content containing email addresses",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 1)]
    public bool DetectEmails { get; set; } = true;

    /// <summary>
    /// Whether to detect phone numbers.
    /// </summary>
    [AIField(
        Label = "Detect Phone Numbers",
        Description = "Flag content containing phone numbers",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 2)]
    public bool DetectPhoneNumbers { get; set; } = true;

    /// <summary>
    /// Whether to detect Social Security Numbers.
    /// </summary>
    [AIField(
        Label = "Detect SSNs",
        Description = "Flag content containing Social Security Numbers",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 3)]
    public bool DetectSSNs { get; set; } = true;

    /// <summary>
    /// Whether to detect credit card numbers.
    /// </summary>
    [AIField(
        Label = "Detect Credit Cards",
        Description = "Flag content containing credit card numbers",
        EditorUiAlias = "Umb.PropertyEditorUi.Toggle",
        SortOrder = 4)]
    public bool DetectCreditCards { get; set; } = true;
}

/// <summary>
/// Guardrail evaluator that detects personally identifiable information (PII) using regex patterns.
/// </summary>
[AIGuardrailEvaluator("pii", "PII Detection", Type = AIGuardrailEvaluatorType.CodeBased)]
public class PIIGuardrailEvaluator : AIGuardrailEvaluatorBase<PIIGuardrailEvaluatorConfig>
{
    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly Regex PhonePattern = new(
        @"\b(?:\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b",
        RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly Regex SSNPattern = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    private static readonly Regex CreditCardPattern = new(
        @"\b(?:\d{4}[-\s]?){3}\d{4}\b",
        RegexOptions.Compiled, TimeSpan.FromSeconds(5));

    /// <inheritdoc />
    public override string Description => "Detects personally identifiable information (emails, phone numbers, SSNs, credit cards)";

    public PIIGuardrailEvaluator(IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    { }

    /// <inheritdoc />
    public override Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken)
    {
        var evalConfig = config.Deserialize<PIIGuardrailEvaluatorConfig>() ?? new PIIGuardrailEvaluatorConfig();
        var detectedTypes = new List<string>();

        if (evalConfig.DetectEmails && EmailPattern.IsMatch(content))
        {
            detectedTypes.Add("email");
        }

        if (evalConfig.DetectPhoneNumbers && PhonePattern.IsMatch(content))
        {
            detectedTypes.Add("phone number");
        }

        if (evalConfig.DetectSSNs && SSNPattern.IsMatch(content))
        {
            detectedTypes.Add("SSN");
        }

        if (evalConfig.DetectCreditCards && CreditCardPattern.IsMatch(content))
        {
            detectedTypes.Add("credit card");
        }

        var flagged = detectedTypes.Count > 0;
        return Task.FromResult(new AIGuardrailResult
        {
            EvaluatorId = Id,
            Flagged = flagged,
            Score = flagged ? 1.0 : 0.0,
            Reason = flagged ? $"PII detected: {string.Join(", ", detectedTypes)}" : null,
            Metadata = flagged
                ? JsonSerializer.SerializeToElement(new { detectedTypes }, Constants.DefaultJsonSerializerOptions)
                : null
        });
    }
}
