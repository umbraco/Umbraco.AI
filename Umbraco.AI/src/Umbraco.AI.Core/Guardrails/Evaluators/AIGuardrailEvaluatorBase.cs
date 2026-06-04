using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Base class for AI guardrail evaluators.
/// Handles attribute reading and provides common infrastructure.
/// </summary>
/// <typeparam name="TConfig">The configuration type for this evaluator.</typeparam>
public abstract class AIGuardrailEvaluatorBase<TConfig> : IAIGuardrailEvaluator
{
    private readonly Lazy<AIEditableModelSchema?> _configSchema;
    private readonly IAIEditableModelResolver _resolver;

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
    [Obsolete("Use the constructor that accepts an IAIGuardrailEvaluatorInfrastructure so that evaluator configuration supports app-settings ($Config) resolution and validation. This constructor will be removed in a future version.")]
    protected AIGuardrailEvaluatorBase(IAIEditableModelSchemaBuilder schemaBuilder)
        : this(schemaBuilder, StaticServiceProvider.Instance.GetRequiredService<IAIEditableModelResolver>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIGuardrailEvaluatorBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The evaluator infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AIGuardrailEvaluatorBase(IAIGuardrailEvaluatorInfrastructure infrastructure)
        : this(infrastructure.SchemaBuilder, infrastructure.ModelResolver)
    {
    }

    private AIGuardrailEvaluatorBase(IAIEditableModelSchemaBuilder schemaBuilder, IAIEditableModelResolver resolver)
    {
        SchemaBuilder = schemaBuilder;
        _resolver = resolver;

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

    /// <summary>
    /// Resolves the strongly-typed evaluator configuration from the supplied <see cref="AIGuardrailConfig"/>,
    /// applying app-settings ($Config) resolution and schema validation through the editable model resolver.
    /// </summary>
    /// <param name="config">The evaluator configuration wrapper.</param>
    /// <returns>The resolved configuration, or <c>null</c> if no configuration is supplied.</returns>
    protected TConfig? ResolveConfig(AIGuardrailConfig config)
    {
        if (config.Config is not { } configElement)
        {
            return default;
        }

        return (TConfig?)_resolver.ResolveModel(typeof(TConfig), configElement, GetConfigSchema());
    }

    /// <inheritdoc />
    public abstract Task<AIGuardrailResult> EvaluateAsync(
        string content,
        IReadOnlyList<ChatMessage> conversationHistory,
        AIGuardrailConfig config,
        CancellationToken cancellationToken);
}
