using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Serializes and deserializes <see cref="AIPropertyPathSegment"/> values matching the LLM-visible
/// JSON shape: bare strings for property aliases, <c>{ "blockKey": "&lt;guid&gt;" }</c> objects for
/// block selectors.
/// </summary>
public sealed class AIPropertyPathSegmentJsonConverter : JsonConverter<AIPropertyPathSegment>
{
    private const string BlockKeyPropertyName = "blockKey";

    /// <inheritdoc />
    public override AIPropertyPathSegment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    var alias = reader.GetString();
                    if (string.IsNullOrEmpty(alias))
                    {
                        throw new JsonException("Property alias path segments cannot be null or empty.");
                    }

                    return AIPropertyPathSegment.ForProperty(alias);
                }

            case JsonTokenType.StartObject:
                {
                    Guid? blockKey = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        if (reader.TokenType != JsonTokenType.PropertyName)
                        {
                            throw new JsonException("Expected property name in path segment object.");
                        }

                        var propertyName = reader.GetString();
                        reader.Read();

                        if (string.Equals(propertyName, BlockKeyPropertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader.TokenType != JsonTokenType.String || !reader.TryGetGuid(out var guid))
                            {
                                throw new JsonException($"'{BlockKeyPropertyName}' must be a GUID string.");
                            }

                            blockKey = guid;
                        }
                        else
                        {
                            // Skip unknown properties to allow forward compatibility with future selectors.
                            reader.Skip();
                        }
                    }

                    if (blockKey is null)
                    {
                        throw new JsonException($"Block key path segments must include a '{BlockKeyPropertyName}' GUID property.");
                    }

                    return AIPropertyPathSegment.ForBlock(blockKey.Value);
                }

            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' for path segment. Expected string or object.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AIPropertyPathSegment value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case AIPropertyPathSegment.PropertyAliasSegment alias:
                writer.WriteStringValue(alias.Alias);
                break;

            case AIPropertyPathSegment.BlockKeySegment block:
                writer.WriteStartObject();
                writer.WriteString(BlockKeyPropertyName, block.BlockKey);
                writer.WriteEndObject();
                break;

            default:
                throw new JsonException($"Unsupported path segment type '{value.GetType().Name}'.");
        }
    }
}
