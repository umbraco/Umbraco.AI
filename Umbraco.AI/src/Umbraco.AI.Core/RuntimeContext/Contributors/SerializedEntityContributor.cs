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
        AIRequestContextItem? matchedItem = null;
        context.RequestContextItems.Handle(IsSerializedEntity, item => matchedItem = item);

        if (matchedItem is null)
        {
            return;
        }

        var entity = PrepareEntity(matchedItem, context);
        if (entity is null)
        {
            return;
        }

        var systemMessage = _contextHelper.FormatForLlm(entity);
        context.SystemMessageParts.Add(systemMessage);
    }

    /// <inheritdoc />
    public async Task ContributeAsync(AIRuntimeContext context, CancellationToken cancellationToken = default)
    {
        AIRequestContextItem? matchedItem = null;
        context.RequestContextItems.Handle(IsSerializedEntity, item => matchedItem = item);

        if (matchedItem is null)
        {
            return;
        }

        var entity = PrepareEntity(matchedItem, context);
        if (entity is null)
        {
            return;
        }

        var systemMessage = await _contextHelper.FormatForLlmAsync(entity, cancellationToken);
        context.SystemMessageParts.Add(systemMessage);
    }

    private bool IsSerializedEntity(AIRequestContextItem item)
    {
        // Check if the value contains entity structure by looking for required fields.
        // Required: entityType (non-empty string), unique (non-empty string), data (object).
        // name is optional — mock entities may not have one.
        // We validate values (not just presence) so mismatched items fall through to
        // other contributors instead of being silently swallowed by Handle's eager-mark.
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return false;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            return value.ValueKind == JsonValueKind.Object
                && HasNonEmptyString(value, "entityType")
                && HasNonEmptyString(value, "unique")
                && value.TryGetProperty("data", out var dataElement)
                && dataElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasNonEmptyString(JsonElement obj, string propertyName)
        => obj.TryGetProperty(propertyName, out var element)
           && element.ValueKind == JsonValueKind.String
           && !string.IsNullOrEmpty(element.GetString());

    /// <summary>
    /// Deserializes the context item's JSON into an <see cref="AISerializedEntity"/>, stores
    /// derived values (entity id, parent id, entity type) into the runtime context's data bag,
    /// and builds template variables from it. Returns <c>null</c> (silently) on any
    /// deserialization failure — the item was already validated as JSON-shaped-like-an-entity
    /// by <see cref="IsSerializedEntity"/>, so failures here are unexpected edge cases, not the
    /// common path.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT call <see cref="IAIEntityContextHelper.FormatForLlm"/> or
    /// <see cref="IAIEntityContextHelper.FormatForLlmAsync"/> — callers do that themselves so
    /// each can use the sync or async path as appropriate.
    /// </remarks>
    private AISerializedEntity? PrepareEntity(AIRequestContextItem item, AIRuntimeContext context)
    {
        if (string.IsNullOrWhiteSpace(item.Value) || !item.Value.DetectIsJson())
        {
            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<JsonElement>(item.Value, _jsonOptions);
            var entity = DeserializeEntity(value);
            if (entity is null)
            {
                return null;
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
            // When an element is present (e.g., block), prefix entity variables with "entity."
            // so they don't collide with element variables. When no element, keep unprefixed.
            var hasElement = context.Data.ContainsKey(Constants.ContextKeys.SerializedElement);
            var variables = _contextHelper.BuildContextDictionary(entity);
            foreach (var (varKey, varValue) in variables)
            {
                if (hasElement)
                {
                    context.Variables[$"entity.{varKey}"] = varValue;
                }
                else
                {
                    context.Variables[varKey] = varValue;
                }
            }

            return entity;
        }
        catch
        {
            // Silently ignore deserialization errors - item wasn't actually an entity
            return null;
        }
    }

    private static AISerializedEntity? DeserializeEntity(JsonElement element)
    {
        // Thorough value validation (called after lightweight IsSerializedEntity check).
        // Required: entityType, unique, data. name is optional (empty string allowed).
        try
        {
            var entityType = element.GetProperty("entityType").GetString();
            var unique = element.GetProperty("unique").GetString();

            if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(unique))
            {
                return null;
            }

            // Extract data field (required)
            if (!element.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Extract name (optional — defaults to empty string)
            string name = string.Empty;
            if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString() ?? string.Empty;
            }

            // Extract parentUnique (optional)
            string? parentUnique = null;
            if (element.TryGetProperty("parentUnique", out var parentUniqueElement))
            {
                parentUnique = parentUniqueElement.GetString();
            }

            // Extract active culture/segment (optional). Frontend adapters emit
            // these on multi-variant entities so the helper can pick matching
            // property values.
            string? culture = null;
            if (element.TryGetProperty("culture", out var cultureElement) && cultureElement.ValueKind == JsonValueKind.String)
            {
                culture = cultureElement.GetString();
            }

            string? segment = null;
            if (element.TryGetProperty("segment", out var segmentElement) && segmentElement.ValueKind == JsonValueKind.String)
            {
                segment = segmentElement.GetString();
            }

            return new AISerializedEntity
            {
                EntityType = entityType,
                Unique = unique,
                Name = name,
                ParentUnique = parentUnique,
                Culture = culture,
                Segment = segment,
                Data = dataElement.Clone() // Clone to avoid referencing original document
            };
        }
        catch
        {
            return null;
        }
    }
}
