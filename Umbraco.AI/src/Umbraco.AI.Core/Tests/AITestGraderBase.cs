using System.Reflection;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test graders.
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestGraderBase<TConfig> : IAITestGrader
{
    private readonly Lazy<AIEditableModelSchema?> _configSchema;
    private readonly IAIEditableModelResolver? _resolver;

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
    [Obsolete("Use the constructor that also accepts an IAIEditableModelResolver so that grader configuration supports app-settings ($Config) resolution and validation. This constructor will be removed in a future version.")]
    protected AITestGraderBase(IAIEditableModelSchemaBuilder schemaBuilder)
        : this(schemaBuilder, resolver: null!)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestGraderBase{TConfig}"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <param name="resolver">The editable model resolver used to resolve configuration values.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AITestGraderBase(IAIEditableModelSchemaBuilder schemaBuilder, IAIEditableModelResolver resolver)
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

        if (_resolver is not null && ConfigType is not null)
        {
            return (TConfig?)_resolver.ResolveModel(ConfigType, configElement, GetConfigSchema());
        }

        // Fallback for graders constructed via the obsolete constructor (no resolver available).
        return configElement.Deserialize<TConfig>(Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    public abstract Task<AITestGraderResult> GradeAsync(
        AITestTranscript transcript,
        AITestOutcome outcome,
        AITestGraderConfig graderConfig,
        CancellationToken cancellationToken);
}
