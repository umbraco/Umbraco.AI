using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Default implementation of <see cref="IAiContextResourceTypeInfrastructure"/>.
/// </summary>
internal sealed class AIContextResourceTypeInfrastructure : IAiContextResourceTypeInfrastructure
{
    /// <inheritdoc />
    public IAiEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <inheritdoc />
    public IAiEditableModelResolver ModelResolver { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextResourceTypeInfrastructure"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The editable model schema builder.</param>
    /// <param name="modelResolver">The editable model resolver.</param>
    public AIContextResourceTypeInfrastructure(
        IAiEditableModelSchemaBuilder schemaBuilder,
        IAiEditableModelResolver modelResolver)
    {
        SchemaBuilder = schemaBuilder;
        ModelResolver = modelResolver;
    }
}
