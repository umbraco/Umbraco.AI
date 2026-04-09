using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Contributes data from context items that contain serialized element data (e.g., a block within a document).
/// Elements are sub-entities like blocks that are edited within a parent entity.
/// Extracts <see cref="AISerializedEntity"/> and populates unprefixed template variables.
/// </summary>
internal sealed class SerializedElementContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IAIEntityContextHelper _contextHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedElementContributor"/> class.
    /// </summary>
    /// <param name="contextHelper">The entity context helper for formatting.</param>
    public SerializedElementContributor(IAIEntityContextHelper contextHelper)
    {
        _contextHelper = contextHelper;
    }

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.Handle(
            IsSerializedElement,
            item => ProcessSerializedElement(item, context));
    }

    private bool IsSerializedElement(AIRequestContextItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("elementType", out _)
                && value.TryGetProperty("unique", out _)
                && value.TryGetProperty("name", out _)
                && value.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private void ProcessSerializedElement(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            var entity = DeserializeElement(value);
            if (entity is null)
            {
                return;
            }

            // Store in data bag
            context.SetValue(Constants.ContextKeys.SerializedElement, entity);

            // Extract element ID as Guid if possible
            if (Guid.TryParse(entity.Unique, out var elementId))
            {
                context.SetValue(Constants.ContextKeys.ElementId, elementId);
            }

            // Store element type
            context.SetValue(Constants.ContextKeys.ElementType, entity.EntityType);

            // Build unprefixed template variables from element properties
            // These are the "local scope" variables — {$heading} resolves from the element
            var variables = _contextHelper.BuildContextDictionary(entity);
            foreach (var (varKey, varValue) in variables)
            {
                context.Variables[varKey] = varValue;
            }

            // Add system message with element context
            var systemMessage = _contextHelper.FormatElementForLlm(entity);
            context.SystemMessageParts.Add(systemMessage);
        }
        catch
        {
            // Silently ignore deserialization errors - item wasn't actually an element
        }
    }

    private static AISerializedEntity? DeserializeElement(JsonElement element)
    {
        try
        {
            // Element context uses "elementType" instead of "entityType"
            var elementType = element.GetProperty("elementType").GetString();
            var unique = element.GetProperty("unique").GetString();
            var name = element.GetProperty("name").GetString();

            if (string.IsNullOrEmpty(elementType) || string.IsNullOrEmpty(unique) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (!element.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new AISerializedEntity
            {
                EntityType = elementType,
                Unique = unique,
                Name = name,
                Data = dataElement.Clone()
            };
        }
        catch
        {
            return null;
        }
    }
}
