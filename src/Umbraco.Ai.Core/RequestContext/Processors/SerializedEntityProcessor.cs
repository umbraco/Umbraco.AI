using System.Text.Json;
using Umbraco.Ai.Core.EntityAdapter;

namespace Umbraco.Ai.Core.RequestContext.Processors;

/// <summary>
/// Processes context items that contain serialized entity data.
/// Extracts <see cref="AiSerializedEntity"/> and populates template variables.
/// </summary>
internal sealed class SerializedEntityProcessor : IAiRequestContextProcessor
{
    private readonly IAiEntityContextHelper _contextHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedEntityProcessor"/> class.
    /// </summary>
    /// <param name="contextHelper">The entity context helper for formatting.</param>
    public SerializedEntityProcessor(IAiEntityContextHelper contextHelper)
    {
        _contextHelper = contextHelper;
    }

    /// <inheritdoc />
    public bool CanHandle(AiRequestContextItem item)
    {
        // Check if the value contains entity structure by looking for entityType and properties
        if (!item.Value.HasValue)
        {
            return false;
        }

        try
        {
            var value = item.Value.Value;
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("entityType", out _)
                && value.TryGetProperty("properties", out _);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Process(AiRequestContextItem item, AiRequestContext context)
    {
        if (!item.Value.HasValue)
        {
            return;
        }

        try
        {
            var entity = DeserializeEntity(item.Value.Value);
            if (entity is null)
            {
                return;
            }

            // Store in data bag
            context.SetData(AiRequestContextKeys.SerializedEntity, entity);

            // Extract entity ID as Guid if possible
            if (Guid.TryParse(entity.Unique, out var entityId))
            {
                context.SetValue(AiRequestContextKeys.EntityId, entityId);
            }

            // Extract parent entity ID as Guid if available (for new entities)
            if (!string.IsNullOrEmpty(entity.ParentUnique) && Guid.TryParse(entity.ParentUnique, out var parentEntityId))
            {
                context.SetValue(AiRequestContextKeys.ParentEntityId, parentEntityId);
            }

            // Store entity type
            context.Data[AiRequestContextKeys.EntityType] = entity.EntityType;

            // Build template variables from entity
            var variables = _contextHelper.BuildContextDictionary(entity);
            foreach (var (key, value) in variables)
            {
                context.Variables[key] = value;
            }

            // Add system message with entity context
            var systemMessage = _contextHelper.FormatForLlm(entity);
            context.SystemMessageParts.Add(systemMessage);
        }
        catch
        {
            // Silently ignore deserialization errors - item wasn't actually an entity
        }
    }

    private static AiSerializedEntity? DeserializeEntity(JsonElement element)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var entityType = element.GetProperty("entityType").GetString();
            var unique = element.GetProperty("unique").GetString();
            var name = element.GetProperty("name").GetString();

            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(unique) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            string? contentType = null;
            if (element.TryGetProperty("contentType", out var contentTypeElement))
            {
                contentType = contentTypeElement.GetString();
            }

            string? parentUnique = null;
            if (element.TryGetProperty("parentUnique", out var parentUniqueElement))
            {
                parentUnique = parentUniqueElement.GetString();
            }

            var properties = new List<AiSerializedProperty>();
            if (element.TryGetProperty("properties", out var propsElement) && propsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var prop in propsElement.EnumerateArray())
                {
                    var alias = prop.TryGetProperty("alias", out var a) ? a.GetString() : null;
                    var label = prop.TryGetProperty("label", out var l) ? l.GetString() : null;
                    var editorAlias = prop.TryGetProperty("editorAlias", out var e) ? e.GetString() : null;

                    if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(editorAlias))
                    {
                        continue;
                    }

                    object? value = null;
                    if (prop.TryGetProperty("value", out var v))
                    {
                        value = ConvertJsonElement(v);
                    }

                    properties.Add(new AiSerializedProperty
                    {
                        Alias = alias,
                        Label = label,
                        EditorAlias = editorAlias,
                        Value = value
                    });
                }
            }

            return new AiSerializedEntity
            {
                EntityType = entityType,
                Unique = unique,
                Name = name,
                ContentType = contentType,
                ParentUnique = parentUnique,
                Properties = properties
            };
        }
        catch
        {
            return null;
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
