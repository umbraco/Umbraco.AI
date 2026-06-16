using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test graders.
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestGraderBase<TConfig> : IAITestGrader
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
    public AIGraderType Type { get; }

    /// <inheritdoc />
    public Type? ConfigType => typeof(TConfig);

    /// <summary>
    /// The schema builder for generating UI schemas.
    /// </summary>
    protected IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestGraderBase{TConfig}"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    [Obsolete("Use the constructor that accepts an IAITestGraderInfrastructure so that grader configuration supports app-settings ($Config) resolution and validation. This constructor will be removed in a future version.")]
    protected AITestGraderBase(IAIEditableModelSchemaBuilder schemaBuilder)
        : this(schemaBuilder, StaticServiceProvider.Instance.GetRequiredService<IAIEditableModelResolver>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestGraderBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The grader infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AITestGraderBase(IAITestGraderInfrastructure infrastructure)
        : this(infrastructure.SchemaBuilder, infrastructure.ModelResolver)
    {
    }

    private AITestGraderBase(IAIEditableModelSchemaBuilder schemaBuilder, IAIEditableModelResolver resolver)
    {
        SchemaBuilder = schemaBuilder;
        _resolver = resolver;

        var attribute = GetType().GetCustomAttribute<AITestGraderAttribute>(inherit: false);
        if (attribute == null)
        {
            throw new InvalidOperationException($"The grader '{GetType().FullName}' is missing the required AITestGraderAttribute.");
        }

        Id = attribute.Id;
        Name = attribute.Name;
        Type = attribute.Type;

        _configSchema = new Lazy<AIEditableModelSchema?>(() => ConfigType != null ? SchemaBuilder.BuildForType(ConfigType, Id) : null);
    }

    /// <inheritdoc />
    public AIEditableModelSchema? GetConfigSchema()
        => _configSchema.Value;

    /// <summary>
    /// Resolves the strongly-typed grader configuration from the stored <see cref="AITestGraderConfig.Config"/>,
    /// applying app-settings ($Config) resolution and schema validation through the editable model resolver.
    /// </summary>
    /// <param name="graderConfig">The stored grader configuration.</param>
    /// <returns>The resolved configuration, or <c>null</c> if no configuration is stored.</returns>
    protected TConfig? ResolveConfig(AITestGraderConfig graderConfig)
    {
        if (graderConfig.Config is not { } configElement)
        {
            return default;
        }

        return (TConfig?)_resolver.ResolveModel(typeof(TConfig), configElement, GetConfigSchema());
    }

    /// <inheritdoc />
    public abstract Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken);
}
