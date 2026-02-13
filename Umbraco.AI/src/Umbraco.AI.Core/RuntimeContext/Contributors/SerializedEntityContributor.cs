using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Contributes data from context items that contain serialized entity data.
/// Extracts <see cref="AISerializedEntity"/> and populates template variables.
/// </summary>
internal sealed class SerializedEntityContributor : IAIRuntimeContextContributor
{
    private readonly JsonSerializerOptions _jsonOptions = new(Constants.DefaultJsonSerializerOptions)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly IAIEntityContextHelper _contextHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedEntityContributor"/> class.
    /// </summary>
    /// <param name="contextHelper">The entity context helper for formatting.</param>
    public SerializedEntityContributor(IAIEntityContextHelper contextHelper)
    {
        _contextHelper = contextHelper;
    }

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.Handle(
            IsSerializedEntity,
            item => ProcessSerializedEntity(item, context));
    }

    private bool IsSerializedEntity(AIRequestContextItem item)
    {
        // Check if the value contains entity structure by looking for required fields
        // Lightweight check: only verifies field presence, not values (performance optimization)
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && value.TryGetProperty("entityType", out _)
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

    private void ProcessSerializedEntity(AIRequestContextItem item, AIRuntimeContext context)
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
            context.SetValue(Constants.ContextKeys.EntityType, entity.EntityType);

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

    private static AISerializedEntity? DeserializeEntity(JsonElement element)
    {
        // Thorough value validation (called after lightweight IsSerializedEntity check)
        try
        {
            var entityType = element.GetProperty("entityType").GetString();
            var unique = element.GetProperty("unique").GetString();
            var name = element.GetProperty("name").GetString();

            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(unique) || string.IsNullOrEmpty(name))
            {
                return null;
            }

            // Extract data field (required)
            if (!element.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Extract parentUnique (optional)
            string? parentUnique = null;
            if (element.TryGetProperty("parentUnique", out var parentUniqueElement))
            {
                parentUnique = parentUniqueElement.GetString();
            }

            return new AISerializedEntity
            {
                EntityType = entityType,
                Unique = unique,
                Name = name,
                ParentUnique = parentUnique,
                Data = dataElement.Clone() // Clone to avoid referencing original document
            };
        }
        catch
        {
            return null;
        }
    }
}
