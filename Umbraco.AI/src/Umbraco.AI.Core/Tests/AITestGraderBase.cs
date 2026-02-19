using System.Reflection;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test graders.
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestGraderBase<TConfig> : IAITestGrader
{
    private readonly Lazy<AIEditableModelSchema?> _configSchema;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public AIGraderType Type { get; }

    /// <inheritdoc />
    public Type? ConfigType => typeof(TConfig);

    /// <summary>
    /// The schema builder for generating UI schemas.
    /// </summary>
    protected IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestGraderBase"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AITestGraderBase(IAIEditableModelSchemaBuilder schemaBuilder)
    {
        SchemaBuilder = schemaBuilder;

        var attribute = GetType().GetCustomAttribute<AITestGraderAttribute>(inherit: false);
        if (attribute == null)
        {
            throw new InvalidOperationException($"The grader '{GetType().FullName}' is missing the required AITestGraderAttribute.");
        }

        Id = attribute.Id;
        Name = attribute.Name;
        Type = attribute.Type;

        _configSchema = new Lazy<AIEditableModelSchema?>(() => BuildSchemaForType(ConfigType, Id));
    }

    /// <inheritdoc />
    public AIEditableModelSchema? GetConfigSchema()
        => _configSchema.Value;

    /// <summary>
    /// Builds a schema for a runtime type using reflection to call the generic method.
    /// </summary>
    private AIEditableModelSchema? BuildSchemaForType(Type? type, string modelId)
    {
        if (type == null)
        {
            return null;
        }

        var method = typeof(IAIEditableModelSchemaBuilder)
            .GetMethod(nameof(IAIEditableModelSchemaBuilder.BuildForType))!
            .MakeGenericMethod(type);

        return (AIEditableModelSchema?)method.Invoke(SchemaBuilder, new object[] { modelId });
    }

    /// <inheritdoc />
    public abstract Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken);
}
