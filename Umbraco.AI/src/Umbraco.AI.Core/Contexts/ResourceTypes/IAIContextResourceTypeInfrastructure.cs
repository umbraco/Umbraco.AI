using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// Defines the infrastructure components required by AI context resource types.
/// </summary>
public interface IAIContextResourceTypeInfrastructure
{
    /// <summary>
    /// Builder for editable model schemas.
    /// </summary>
    IAiEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Resolver for converting stored data to typed models.
    /// </summary>
    IAiEditableModelResolver ModelResolver { get; }
}
