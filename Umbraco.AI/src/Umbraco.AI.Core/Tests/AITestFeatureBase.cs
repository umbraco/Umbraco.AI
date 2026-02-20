using System.Reflection;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for AI test features (harnesses).
/// Handles attribute reading and provides common infrastructure.
/// </summary>
public abstract class AITestFeatureBase<TConfig> : IAITestFeature
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
    /// Initializes a new instance of the <see cref="AITestFeatureBase{TConfig}"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The schema builder.</param>
    /// <exception cref="InvalidOperationException">Thrown if the class is missing the required attribute.</exception>
    protected AITestFeatureBase(IAIEditableModelSchemaBuilder schemaBuilder)
    {
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
    public abstract Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        CancellationToken cancellationToken);
}
