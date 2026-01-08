using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Default implementation of <see cref="IAiContextResourceTypeInfrastructure"/>.
/// </summary>
internal sealed class AiContextResourceTypeInfrastructure : IAiContextResourceTypeInfrastructure
{
    /// <inheritdoc />
    public IAiEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <inheritdoc />
    public IAiEditableModelResolver ModelResolver { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextResourceTypeInfrastructure"/> class.
    /// </summary>
    /// <param name="schemaBuilder">The editable model schema builder.</param>
    /// <param name="modelResolver">The editable model resolver.</param>
    public AiContextResourceTypeInfrastructure(
        IAiEditableModelSchemaBuilder schemaBuilder,
        IAiEditableModelResolver modelResolver)
    {
        SchemaBuilder = schemaBuilder;
        ModelResolver = modelResolver;
    }
}
