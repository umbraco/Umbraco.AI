using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Versionable entity adapter for AI connections.
/// </summary>
/// <remarks>
/// Handles encryption/decryption of sensitive settings during snapshot operations.
/// </remarks>
internal sealed class AIConnectionVersionableEntityAdapter : AIVersionableEntityAdapterBase<AIConnection>
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIProviderCollection _providers;
    private readonly IAIConnectionService _connectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIConnectionVersionableEntityAdapter"/> class.
    /// </summary>
    /// <param name="serializer">The serializer for handling encrypted settings.</param>
    /// <param name="providers">The provider collection for retrieving settings schemas.</param>
    /// <param name="connectionService">The connection service for rollback operations.</param>
    public AIConnectionVersionableEntityAdapter(
        IAIEditableModelSerializer serializer,
        AIProviderCollection providers,
        IAIConnectionService connectionService)
    {
        _serializer = serializer;
        _providers = providers;
        _connectionService = connectionService;
    }

    /// <inheritdoc />
    protected override string CreateSnapshot(AIConnection entity)
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
    protected override AIConnection? RestoreFromSnapshot(string json)
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

            return new AIConnection
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
    protected override IReadOnlyList<AIPropertyChange> CompareVersions(AIConnection from, AIConnection to)
    {
        var changes = new List<AIPropertyChange>();

        if (from.Alias != to.Alias)
        {
            changes.Add(new AIPropertyChange("Alias", from.Alias, to.Alias));
        }

        if (from.Name != to.Name)
        {
            changes.Add(new AIPropertyChange("Name", from.Name, to.Name));
        }

        if (from.ProviderId != to.ProviderId)
        {
            changes.Add(new AIPropertyChange("ProviderId", from.ProviderId, to.ProviderId));
        }

        if (from.IsActive != to.IsActive)
        {
            changes.Add(new AIPropertyChange("IsActive", from.IsActive.ToString(), to.IsActive.ToString()));
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
    private void CompareSettings(object? from, object? to, string providerId, List<AIPropertyChange> changes)
    {
        // Get provider schema to identify sensitive fields
        var schema = GetSchemaForProvider(providerId);

        // Create sensitivity checker based on schema
        bool IsSensitive(string path)
        {
            var propertyName = path.Split('.').LastOrDefault() ?? path;
            return schema?.Fields.Any(f =>
                (string.Equals(f.Key, propertyName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(f.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase)) &&
                f.IsSensitive) ?? false;
        }

        // Use shared JSON comparison utility
        var success = AIJsonComparer.CompareObjects(from, to, "Settings", changes, IsSensitive);

        if (!success && !Equals(from, to))
        {
            // Fallback if comparison failed
            changes.Add(new AIPropertyChange("Settings", "(modified)", "(modified)"));
        }
    }

    /// <inheritdoc />
    public override Task RollbackAsync(Guid entityId, int version, CancellationToken cancellationToken = default)
        => _connectionService.RollbackConnectionAsync(entityId, version, cancellationToken);

    /// <inheritdoc />
    protected override Task<AIConnection?> GetEntityAsync(Guid entityId, CancellationToken cancellationToken)
        => _connectionService.GetConnectionAsync(entityId, cancellationToken);

    private AIEditableModelSchema? GetSchemaForProvider(string providerId)
    {
        var provider = _providers.GetById(providerId);
        return provider?.GetSettingsSchema();
    }
}
