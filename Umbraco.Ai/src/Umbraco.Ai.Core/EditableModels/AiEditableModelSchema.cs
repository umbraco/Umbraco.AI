namespace Umbraco.Ai.Core.EditableModels;

/// <summary>
/// Represents the schema of an editable model, including its type and field definitions.
/// </summary>
public class AiEditableModelSchema
{
    /// <summary>
    /// Creates a new instance of <see cref="AiEditableModelSchema"/>.
    /// </summary>
    /// <param name="type">The type of the model.</param>
    /// <param name="fields">The field definitions for the model.</param>
    public AiEditableModelSchema(Type type, IReadOnlyList<AiEditableModelField> fields)
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
    public IReadOnlyList<AiEditableModelField> Fields { get; }
}
