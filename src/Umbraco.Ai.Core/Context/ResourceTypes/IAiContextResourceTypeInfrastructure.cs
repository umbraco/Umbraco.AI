using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Defines the infrastructure components required by AI context resource types.
/// </summary>
public interface IAiContextResourceTypeInfrastructure
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
