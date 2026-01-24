using System.Text.Json;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Versioning;

namespace Umbraco.Ai.Core.Connections;

/// <summary>
/// Versionable entity adapter for AI connections.
/// </summary>
/// <remarks>
/// Handles encryption/decryption of sensitive settings during snapshot operations.
/// </remarks>
internal sealed class AiConnectionVersionableEntityAdapter : AiVersionableEntityAdapterBase<AiConnection>
{
    private readonly IAiEditableModelSerializer _serializer;
    private readonly AiProviderCollection _providers;
    private readonly IAiConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiConnectionVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer for handling encrypted settings.</param>
    /// <param name="providers">The provider collection for retrieving settings schemas.</param>
    /// <param name="connectionService">The connection service for rollback operations.</param>
    public AiConnectionVersionableEntityAdapter(
        IAiEditableModelSerializer serializer,
        AiProviderCollection providers,
        IAiConnectionService connectionService)
    {
        _serializer = serializer;
        _providers = providers;
        _connectionService = connectionService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AiConnection entity)
    {
        // Create a snapshot with encrypted settings
        var schema = GetSchemaForProvider(entity.ProviderId);
        var encryptedSettings = _serializer.Serialize(entity.Settings, schema);

        var snapshot = new
        {
            entity.Id,
            entity.Alias,
            entity.Name,
            entity.ProviderId,
            Settings = encryptedSettings, // Encrypted JSON string
            entity.IsActive,
            entity.Version,
            entity.DateCreated,
            entity.DateModified,
            entity.CreatedByUserId,
            entity.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    protected override AiConnection? RestoreFromSnapshot(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            // Parse the snapshot JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Decrypt settings (the serializer handles ENC: prefixed values)
            object? settings = null;
            if (root.TryGetProperty("settings", out var settingsElement) &&
                settingsElement.ValueKind == JsonValueKind.String)
            {
                var settingsJson = settingsElement.GetString();
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    settings = _serializer.Deserialize(settingsJson);
                }
            }

            return new AiConnection
            {
                Id = root.GetProperty("id").GetGuid(),
                Alias = root.GetProperty("alias").GetString()!,
                Name = root.GetProperty("name").GetString()!,
                ProviderId = root.GetProperty("providerId").GetString()!,
                Settings = settings,
                IsActive = root.GetProperty("isActive").GetBoolean(),
                Version = root.GetProperty("version").GetInt32(),
                DateCreated = root.GetProperty("dateCreated").GetDateTime(),
                DateModified = root.GetProperty("dateModified").GetDateTime(),
                // Try Guid first (new format), ignore old int values (no conversion path)
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null && cbu.TryGetGuid(out var cbuGuid)
                    ? cbuGuid : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null && mbu.TryGetGuid(out var mbuGuid)
                    ? mbuGuid : null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<AiPropertyChange> CompareVersions(AiConnection from, AiConnection to)
    {
        var changes = new List<AiPropertyChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AiPropertyChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AiPropertyChange("Name", from.Name, to.Name));
        }

        if (from.ProviderId != to.ProviderId)
        {
            changes.Add(new AiPropertyChange("ProviderId", from.ProviderId, to.ProviderId));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AiPropertyChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
        }

        // Compare settings with deep inspection
        CompareSettings(from.Settings, to.Settings, from.ProviderId, changes);

        return changes;
    }

    /// <summary>
    /// Compares connection settings using JSON serialization for accurate deep comparison.
    /// </summary>
    /// <remarks>
    /// Since connection settings are untyped (object?), we use JSON serialization to compare
    /// the actual property values. Sensitive values are masked in the change output.
    /// </remarks>
    private void CompareSettings(object? from, object? to, string providerId, List<AiPropertyChange> changes)
    {
        // Handle null cases
        if (from == null && to == null)
        {
            return;
        }

        if (from == null)
        {
            changes.Add(new AiPropertyChange("Settings", null, "(configured)"));
            return;
        }

        if (to == null)
        {
            changes.Add(new AiPropertyChange("Settings", "(configured)", null));
            return;
        }

        // Get provider schema to identify sensitive fields
        var schema = GetSchemaForProvider(providerId);

        // Serialize both settings to JSON for comparison
        // Use non-encrypted serialization for comparison (we don't want to compare encrypted values)
        string fromJson, toJson;
        try
        {
            fromJson = JsonSerializer.Serialize(from, from.GetType(), Constants.DefaultJsonSerializerOptions);
            toJson = JsonSerializer.Serialize(to, to.GetType(), Constants.DefaultJsonSerializerOptions);
        }
        catch
        {
            // Fallback to simple check if serialization fails
            if (!Equals(from, to))
            {
                changes.Add(new AiPropertyChange("Settings", "(modified)", "(modified)"));
            }

            return;
        }

        // If JSON is identical, no changes
        if (fromJson == toJson)
        {
            return;
        }

        // Parse JSON to find specific property changes
        try
        {
            using var fromDoc = JsonDocument.Parse(fromJson);
            using var toDoc = JsonDocument.Parse(toJson);

            CompareJsonElements(
                fromDoc.RootElement,
                toDoc.RootElement,
                "Settings",
                schema,
                changes);
        }
        catch
        {
            // Fallback: just indicate settings changed
            changes.Add(new AiPropertyChange("Settings", "(modified)", "(modified)"));
        }
    }

    /// <summary>
    /// Compares two JSON elements and reports property-level changes.
    /// </summary>
    private static void CompareJsonElements(
        JsonElement from,
        JsonElement to,
        string path,
        AiEditableModelSchema? schema,
        List<AiPropertyChange> changes)
    {
        // Handle different value kinds
        if (from.ValueKind != to.ValueKind)
        {
            AddChange(path, from, to, schema, changes);
            return;
        }

        switch (from.ValueKind)
        {
            case JsonValueKind.Object:
                CompareJsonObjects(from, to, path, schema, changes);
                break;

            case JsonValueKind.Array:
                CompareJsonArrays(from, to, path, schema, changes);
                break;

            default:
                // Primitive value comparison
                if (from.GetRawText() != to.GetRawText())
                {
                    AddChange(path, from, to, schema, changes);
                }

                break;
        }
    }

    /// <summary>
    /// Compares two JSON objects and reports property changes.
    /// </summary>
    private static void CompareJsonObjects(
        JsonElement from,
        JsonElement to,
        string path,
        AiEditableModelSchema? schema,
        List<AiPropertyChange> changes)
    {
        var fromProps = from.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        var toProps = to.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

        // Check for added and modified properties
        foreach (var (name, toValue) in toProps)
        {
            var propPath = $"{path}.{name}";

            if (!fromProps.TryGetValue(name, out var fromValue))
            {
                // Property added
                AddChange(propPath, default, toValue, schema, changes);
            }
            else
            {
                // Property exists in both - compare recursively
                CompareJsonElements(fromValue, toValue, propPath, schema, changes);
            }
        }

        // Check for removed properties
        foreach (var (name, fromValue) in fromProps)
        {
            if (!toProps.ContainsKey(name))
            {
                var propPath = $"{path}.{name}";
                AddChange(propPath, fromValue, default, schema, changes);
            }
        }
    }

    /// <summary>
    /// Compares two JSON arrays.
    /// </summary>
    private static void CompareJsonArrays(
        JsonElement from,
        JsonElement to,
        string path,
        AiEditableModelSchema? schema,
        List<AiPropertyChange> changes)
    {
        var fromArray = from.EnumerateArray().ToList();
        var toArray = to.EnumerateArray().ToList();

        // Simple comparison: report if arrays differ
        if (fromArray.Count != toArray.Count ||
            from.GetRawText() != to.GetRawText())
        {
            changes.Add(new AiPropertyChange(
                path,
                $"[{fromArray.Count} items]",
                $"[{toArray.Count} items]"));
        }
    }

    /// <summary>
    /// Adds a property change, masking sensitive values.
    /// </summary>
    private static void AddChange(
        string path,
        JsonElement from,
        JsonElement to,
        AiEditableModelSchema? schema,
        List<AiPropertyChange> changes)
    {
        // Extract property name from path for schema lookup
        var propertyName = path.Split('.').LastOrDefault() ?? path;
        var isSensitive = schema?.Fields.Any(f =>
            string.Equals(f.Alias, propertyName, StringComparison.OrdinalIgnoreCase) &&
            f.IsSensitive) ?? false;

        string? fromValue, toValue;

        if (isSensitive)
        {
            // Mask sensitive values but indicate if they changed
            fromValue = from.ValueKind != JsonValueKind.Undefined && from.ValueKind != JsonValueKind.Null
                ? "********"
                : null;
            toValue = to.ValueKind != JsonValueKind.Undefined && to.ValueKind != JsonValueKind.Null
                ? "********"
                : null;
        }
        else
        {
            fromValue = FormatJsonValue(from);
            toValue = FormatJsonValue(to);
        }

        changes.Add(new AiPropertyChange(path, fromValue, toValue));
    }

    /// <summary>
    /// Formats a JSON element value for display.
    /// </summary>
    private static string? FormatJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.Null => null,
            JsonValueKind.String => TruncateValue(element.GetString()),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
            JsonValueKind.Object => "(object)",
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Truncates a value for display in change logs.
    /// </summary>
    private static string? TruncateValue(string? value, int maxLength = 100)
    {
        if (value == null)
        {
            return null;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength) + "...";
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _connectionService.RollbackConnectionAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AiConnection?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _connectionService.GetConnectionAsync(entityId, cancellationToken);

    private AiEditableModelSchema? GetSchemaForProvider(string providerId)
    {
        var provider = _providers.GetById(providerId);
        return provider?.GetSettingsSchema();
    }
}
