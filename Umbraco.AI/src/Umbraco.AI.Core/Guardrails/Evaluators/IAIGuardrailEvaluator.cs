using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Attribute to mark AI guardrail evaluator implementations.
/// Evaluators validate content for safety, compliance, and quality at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AIGuardrailEvaluatorAttribute(string id, string name) : Attribute
{
    /// <summary>
    /// The unique identifier of the evaluator.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// The display name of the evaluator.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The type of evaluator (CodeBased or ModelBased).
    /// </summary>
    public AIGuardrailEvaluatorType Type { get; set; } = AIGuardrailEvaluatorType.CodeBased;
}

/// <summary>
/// Defines a guardrail evaluator that validates content for safety and compliance.
/// </summary>
public interface IAIGuardrailEvaluator : IDiscoverable
{
    /// <summary>
    /// The unique identifier of the evaluator.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name of the evaluator.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description of what this evaluator checks.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The type of evaluator (CodeBased or ModelBased).
    /// </summary>
    AIGuardrailEvaluatorType Type { get; }

    /// <summary>
    /// The type that represents the evaluator configuration.
    /// Returns null if no configuration is needed.
    /// </summary>
    Type? ConfigType { get; }

    /// <summary>
    /// Gets the configuration schema for UI rendering.
    /// Returns null if no configuration is needed.
    /// </summary>
    AIEditableModelSchema? GetConfigSchema();

    /// <summary>
    /// Evaluates content against this guardrail evaluator.
    /// </summary>
    /// <param name="content">The text content to evaluate (input or response).</param>
    /// <param name="conversationHistory">The conversation history for context.</param>
    /// <param name="config">The evaluator-specific configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken);
}
