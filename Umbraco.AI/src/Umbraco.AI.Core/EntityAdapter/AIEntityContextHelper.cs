using System.Text.Json;

namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Default implementation of <see cref="IAIEntityContextHelper"/>.
/// </summary>
internal sealed class AIEntityContextHelper : IAIEntityContextHelper
{
    private readonly AIEntityFormatterCollection _formatters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIEntityContextHelper"/> class.
    /// </summary>
    /// <param name="formatters">The entity formatter collection.</param>
    public AIEntityContextHelper(AIEntityFormatterCollection formatters)
    {
        _formatters = formatters;
    }

    /// <inheritdoc />
    public Dictionary<string, object?> BuildContextDictionary(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var context = new Dictionary<string, object?>
        {
            ["entityType"] = entity.EntityType,
            ["entityId"] = entity.Unique,
            ["entityName"] = entity.Name,
        };

        // Extract contentType from data if present (CMS entities)
        if (entity.Data.ValueKind == JsonValueKind.Object &&
            entity.Data.TryGetProperty("contentType", out var contentTypeElement) &&
            contentTypeElement.ValueKind == JsonValueKind.String)
        {
            context["contentType"] = contentTypeElement.GetString();
        }

        // Extract property values from data.properties array if present (CMS entities)
        if (entity.Data.ValueKind == JsonValueKind.Object &&
            entity.Data.TryGetProperty("properties", out var propertiesElement) &&
            propertiesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var propElement in propertiesElement.EnumerateArray())
            {
                if (propElement.ValueKind != JsonValueKind.Object)
                    continue;

                // Get alias and value
                if (propElement.TryGetProperty("alias", out var aliasElement) &&
                    aliasElement.ValueKind == JsonValueKind.String)
                {
                    var alias = aliasElement.GetString();
                    if (string.IsNullOrEmpty(alias))
                        continue;

                    // Extract value
                    object? value = null;
                    if (propElement.TryGetProperty("value", out var valueElement))
                    {
                        value = ExtractValue(valueElement);
                    }

                    context[alias] = value;
                }
            }
        }

        return context;
    }

    /// <inheritdoc />
    public string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Get the appropriate formatter for this entity type
        var formatter = _formatters.GetFormatter(entity.EntityType);

        return formatter.Format(entity);
    }

    private static object? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(), // For objects/arrays, return JSON string
        };
    }
}
