using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test features (harnesses).
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestFeatureBase<TConfig> : IAITestFeature
    where TConfig : AITestFeatureConfigBase
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
    public string Category { get; }

    /// <inheritdoc />
    public virtual Type? ConfigType => typeof(TConfig);

    /// <summary>
    /// The schema builder for generating UI schemas.
    /// </summary>
    protected IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// The context resolver for resolving mock entity context items.
    /// </summary>
    protected AITestContextResolver ContextResolver { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestFeatureBase{TConfig}"/> class.
    /// </summary>
    /// <param name="contextResolver">The context resolver.</param>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    [Obsolete("Use the constructor that accepts an IAITestFeatureInfrastructure so that test feature configuration supports app-settings ($Config) resolution and validation. This constructor will be removed in a future version.")]
    protected AITestFeatureBase(AITestContextResolver contextResolver, IAIEditableModelSchemaBuilder schemaBuilder)
        : this(contextResolver, schemaBuilder, StaticServiceProvider.Instance.GetRequiredService<IAIEditableModelResolver>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AITestFeatureBase{TConfig}"/> class.
    /// </summary>
    /// <param name="infrastructure">The test feature infrastructure dependencies.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AITestFeatureBase(IAITestFeatureInfrastructure infrastructure)
        : this(infrastructure.ContextResolver, infrastructure.SchemaBuilder, infrastructure.ModelResolver)
    {
    }

    private AITestFeatureBase(AITestContextResolver contextResolver, IAIEditableModelSchemaBuilder schemaBuilder, IAIEditableModelResolver resolver)
    {
        ContextResolver = contextResolver;
        SchemaBuilder = schemaBuilder;
        _resolver = resolver;

        var attribute = GetType().GetCustomAttribute<AITestFeatureAttribute>(inherit: false);
        if (attribute == null)
        {
            throw new InvalidOperationException($"The test feature '{GetType().FullName}' is missing the required AITestFeatureAttribute.");
        }

        Id = attribute.Id;
        Name = attribute.Name;
        Category = attribute.Category;

        _configSchema = new Lazy<AIEditableModelSchema?>(() => ConfigType != null ? SchemaBuilder.BuildForType(ConfigType, Id) : null);
    }

    /// <inheritdoc />
    public AIEditableModelSchema? GetConfigSchema()
        => _configSchema.Value;

    /// <summary>
    /// Resolves the strongly-typed test feature configuration from the test's stored
    /// <see cref="AITest.TestFeatureConfig"/>, applying app-settings ($Config) resolution and schema
    /// validation through the editable model resolver.
    /// </summary>
    /// <param name="test">The test whose feature configuration should be resolved.</param>
    /// <returns>The resolved configuration, or <c>null</c> if no configuration is stored.</returns>
    protected TConfig? ResolveTestFeatureConfig(AITest test)
    {
        if (test.TestFeatureConfig is not { } configElement)
        {
            return null;
        }

        return (TConfig?)_resolver.ResolveModel(typeof(TConfig), configElement, GetConfigSchema());
    }

    /// <inheritdoc />
    /// <remarks>
    /// Default implementation extracts the "content" property from the transcript's FinalOutput.
    /// Override in derived classes for entity-specific extraction logic.
    /// </remarks>
    public virtual string ExtractOutputValue(AITestTranscript transcript)
    {
        var output = transcript.FinalOutput;

        if (output.ValueKind == JsonValueKind.Undefined)
        {
            return string.Empty;
        }

        if (output.TryGetProperty("content", out var content))
        {
            return content.GetString() ?? string.Empty;
        }

        return output.GetRawText();
    }

    /// <inheritdoc />
    public abstract Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        IEnumerable<Guid>? guardrailIdsOverride,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deserializes the entity context from the config.
    /// </summary>
    protected EntityContextConfig? ResolveEntityContext(TConfig config)
    {
        if (config.EntityContext is not { } element)
        {
            return null;
        }

        // The mock entity editor stores values as JSON strings (double-encoded).
        // If the element is a string, deserialize from the string content directly.
        if (element.ValueKind == JsonValueKind.String)
        {
            var json = element.GetString();
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<EntityContextConfig>(json, Constants.DefaultJsonSerializerOptions);
        }

        return element.Deserialize<EntityContextConfig>(Constants.DefaultJsonSerializerOptions);
    }

    /// <summary>
    /// Resolves mock entity context items from the config.
    /// </summary>
    protected List<AIRequestContextItem> ResolveEntityContextItems(TConfig config)
    {
        var entityContext = ResolveEntityContext(config);
        return ContextResolver.ResolveContextItems(entityContext?.MockEntity);
    }

}
