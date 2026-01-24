using System.Text.Json;

namespace Umbraco.Ai.Core.EditableModels;

/// <summary>
/// Serializes and deserializes editable model objects with automatic encryption of sensitive fields.
/// </summary>
public interface IAiEditableModelSerializer
{
    /// <summary>
    /// Serializes an editable model object to JSON, encrypting sensitive field values based on the schema.
    /// </summary>
    /// <param name="model">The model object to serialize.</param>
    /// <param name="schema">The schema describing which fields are sensitive. If null, no encryption is applied.</param>
    /// <returns>JSON string with sensitive values encrypted.</returns>
    string? Serialize(object? model, AiEditableModelSchema? schema);

    /// <summary>
    /// Deserializes a JSON string, decrypting any encrypted field values.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="object"/> with decrypted values.</returns>
    object Deserialize(string? json);
}
