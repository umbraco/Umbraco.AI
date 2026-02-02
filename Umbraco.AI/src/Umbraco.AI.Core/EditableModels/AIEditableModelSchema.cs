namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Represents the schema of an editable model, including its type and field definitions.
/// </summary>
public class AIEditableModelSchema
{
    /// <summary>
    /// Creates a new instance of <see cref="AIEditableModelSchema"/>.
    /// </summary>
    /// <param name="type">The type of the model.</param>
    /// <param name="fields">The field definitions for the model.</param>
    public AIEditableModelSchema(Type type, IReadOnlyList<AIEditableModelField> fields)
    {
        Type = type;
        Fields = fields;
    }

    /// <summary>
    /// The type of the editable model.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The field definitions for the editable model.
    /// </summary>
    public IReadOnlyList<AIEditableModelField> Fields { get; }
}
