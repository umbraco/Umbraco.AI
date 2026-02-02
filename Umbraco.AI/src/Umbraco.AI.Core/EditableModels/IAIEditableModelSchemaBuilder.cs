namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Defines a contract for building editable model schemas from types.
/// </summary>
public interface IAIEditableModelSchemaBuilder
{
    /// <summary>
    /// Builds a schema for the specified model type.
    /// </summary>
    /// <param name="modelId">An identifier for the model (e.g., provider ID).</param>
    /// <typeparam name="TModel">The model type to build a schema for.</typeparam>
    /// <returns>The schema containing field definitions for the model.</returns>
    AIEditableModelSchema BuildForType<TModel>(string modelId)
        where TModel : class;
}
