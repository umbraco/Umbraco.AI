using System.Reflection;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Base class for AI guardrail evaluators.
/// Handles attribute reading and provides common infrastructure.
/// </summary>
/// <typeparam name="TConfig">The configuration type for this evaluator.</typeparam>
public abstract class AIGuardrailEvaluatorBase<TConfig> : IAIGuardrailEvaluator
{
    private readonly Lazy<AIEditableModelSchema?> _configSchema;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public AIGuardrailEvaluatorType Type { get; }

    /// <inheritdoc />
    public Type? ConfigType => typeof(TConfig);

    /// <summary>
    /// The schema builder for generating UI schemas.
    /// </summary>
    protected IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailEvaluatorBase{TConfig}"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AIGuardrailEvaluatorBase(IAIEditableModelSchemaBuilder schemaBuilder)
    {
        SchemaBuilder = schemaBuilder;

        var attribute = GetType().GetCustomAttribute<AIGuardrailEvaluatorAttribute>(inherit: false);
        if (attribute == null)
        {
            throw new InvalidOperationException(
                $"The evaluator '{GetType().FullName}' is missing the required AIGuardrailEvaluatorAttribute.");
        }

        Id = attribute.Id;
        Name = attribute.Name;
        Type = attribute.Type;

        _configSchema = new Lazy<AIEditableModelSchema?>(
            () => ConfigType != null ? SchemaBuilder.BuildForType(ConfigType, Id) : null);
    }

    /// <inheritdoc />
    public AIEditableModelSchema? GetConfigSchema()
        => _configSchema.Value;

    /// <inheritdoc />
    public abstract Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken);
}
