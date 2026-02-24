using System.Reflection;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test features (harnesses).
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestFeatureBase<TConfig> : IAITestFeature
    where TConfig : AITestFeatureConfigBase
{
    private readonly Lazy<AIEditableModelSchema?> _configSchema;

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
    protected AITestFeatureBase(AITestContextResolver contextResolver, IAIEditableModelSchemaBuilder schemaBuilder)
    {
        ContextResolver = contextResolver;
        SchemaBuilder = schemaBuilder;

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

    /// <inheritdoc />
    /// <remarks>
    /// Default implementation extracts the "content" property from the transcript's FinalOutputJson.
    /// Override in derived classes for entity-specific extraction logic.
    /// </remarks>
    public virtual string ExtractOutputValue(AITestTranscript transcript)
    {
        var outputJson = transcript.FinalOutputJson;

        if (string.IsNullOrWhiteSpace(outputJson))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(outputJson);
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // If parsing fails, return raw value
        }

        return outputJson;
    }

    /// <inheritdoc />
    public abstract Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
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

    /// <summary>
    /// Returns the effective context IDs, preferring per-run override over config value.
    /// </summary>
    protected IEnumerable<Guid>? ResolveEffectiveContextIds(TConfig config, IEnumerable<Guid>? contextIdsOverride)
        => contextIdsOverride ?? config.ContextIds;
}
