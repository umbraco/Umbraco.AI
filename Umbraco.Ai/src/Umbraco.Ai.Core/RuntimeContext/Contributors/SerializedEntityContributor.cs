using System.Text.Json;
using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Extensions;

namespace Umbraco.Ai.Core.RuntimeContext.Contributors;

/// <summary>
/// Contributes data from context items that contain serialized entity data.
/// Extracts <see cref="AiSerializedEntity"/> and populates template variables.
/// </summary>
internal sealed class SerializedEntityContributor : IAiRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IAiEntityContextHelper _contextHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedEntityContributor"/> class.
    /// </summary>
    /// <param name="contextHelper">The entity context helper for formatting.</param>
    public SerializedEntityContributor(IAiEntityContextHelper contextHelper)
    {
        _contextHelper = contextHelper;
    }

    /// <inheritdoc />
    public void Contribute(AiRuntimeContext context)
    {
        context.HandleRequestContextItem(
            IsSerializedEntity,
            item => ProcessSerializedEntity(item, context));
    }

    private bool IsSerializedEntity(AiRequestContextItem item)
    {
        // Check if the value contains entity structure by looking for entityType and properties
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("entityType", out _)
                && value.TryGetProperty("properties", out _);
        }
        catch
        {
            return false;
        }
    }

    private void ProcessSerializedEntity(AiRequestContextItem item, AiRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            var entity = DeserializeEntity(value);
            if (entity is null)
            {
                return;
            }

            // Store in data bag
            context.SetValue(Constants.ContextKeys.SerializedEntity, entity);

            // Extract entity ID as Guid if possible
            if (Guid.TryParse(entity.Unique, out var entityId))
            {
                context.SetValue(Constants.ContextKeys.EntityId, entityId);
            }

            // Extract parent entity ID as Guid if available (for new entities)
            if (!string.IsNullOrEmpty(entity.ParentUnique) && Guid.TryParse(entity.ParentUnique, out var parentEntityId))
            {
                context.SetValue(Constants.ContextKeys.ParentEntityId, parentEntityId);
            }

            // Store entity type
            context.Data[Constants.ContextKeys.EntityType] = entity.EntityType;

            // Build template variables from entity
            var variables = _contextHelper.BuildContextDictionary(entity);
            foreach (var (varKey, varValue) in variables)
            {
                context.Variables[varKey] = varValue;
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
            _ => element
        };
    }
}
