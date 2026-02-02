using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.AI.Core.Security;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Serializes and deserializes editable model objects with automatic encryption of sensitive fields.
/// </summary>
internal sealed class AIEditableModelSerializer : IAIEditableModelSerializer
{
    private readonly IAISensitiveFieldProtector _protector;

    public AIEditableModelSerializer(IAISensitiveFieldProtector protector)
    {
        _protector = protector;
    }

    /// <inheritdoc />
    public string? Serialize(object? model, AIEditableModelSchema? schema)
    {
        if (model is null)
        {
            return null;
        }

        // Serialize to JSON
        var json = JsonSerializer.Serialize(model, Constants.DefaultJsonSerializerOptions);

        // If no schema or no sensitive fields, return as-is
        if (schema is null || !schema.Fields.Any(f => f.IsSensitive))
        {
            return json;
        }

        // Parse JSON and encrypt sensitive fields
        var jsonNode = JsonNode.Parse(json);
        if (jsonNode is JsonObject jsonObject)
        {
            EncryptSensitiveFields(jsonObject, schema);
            return jsonObject.ToJsonString(Constants.DefaultJsonSerializerOptions);
        }

        return json;
    }

    /// <inheritdoc />
    public object Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default(JsonElement);
        }

        // Parse JSON and decrypt encrypted fields
        var jsonNode = JsonNode.Parse(json);
        if (jsonNode is JsonObject jsonObject)
        {
            DecryptFields(jsonObject);
            var decryptedJson = jsonObject.ToJsonString(Constants.DefaultJsonSerializerOptions);
            return JsonSerializer.Deserialize<JsonElement>(decryptedJson, Constants.DefaultJsonSerializerOptions);
        }

        return JsonSerializer.Deserialize<JsonElement>(json, Constants.DefaultJsonSerializerOptions);
    }

    private void EncryptSensitiveFields(JsonObject jsonObject, AIEditableModelSchema schema)
    {
        var sensitiveKeys = schema.Fields
            .Where(f => f.IsSensitive)
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var property in jsonObject.ToList())
        {
            if (sensitiveKeys.Contains(property.Key) && property.Value is JsonValue jsonValue)
            {
                var stringValue = jsonValue.GetValue<string>();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    var encrypted = _protector.Protect(stringValue);
                    jsonObject[property.Key] = encrypted;
                }
            }
        }
    }

    private void DecryptFields(JsonObject jsonObject)
    {
        foreach (var property in jsonObject.ToList())
        {
            if (property.Value is JsonValue jsonValue)
            {
                // Try to get as string - if it starts with ENC:, decrypt it
                try
                {
                    var stringValue = jsonValue.GetValue<string>();
                    if (_protector.IsProtected(stringValue))
                    {
                        var decrypted = _protector.Unprotect(stringValue);
                        jsonObject[property.Key] = decrypted;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Not a string value, skip
                }
            }
            else if (property.Value is JsonObject nestedObject)
            {
                // Recursively handle nested objects
                DecryptFields(nestedObject);
            }
        }
    }
}
