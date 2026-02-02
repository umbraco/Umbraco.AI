using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Default implementation of <see cref="IAIContextResourceTypeInfrastructure"/>.
/// </summary>
internal sealed class AIContextResourceTypeInfrastructure : IAIContextResourceTypeInfrastructure
{
    /// <inheritdoc />
    public IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <inheritdoc />
    public IAIEditableModelResolver ModelResolver { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeInfrastructure"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The editable model schema builder.</param>
    /// <param name="modelResolver">The editable model resolver.</param>
    public AIContextResourceTypeInfrastructure(
        IAIEditableModelSchemaBuilder schemaBuilder,
        IAIEditableModelResolver modelResolver)
    {
        SchemaBuilder = schemaBuilder;
        ModelResolver = modelResolver;
    }
}
