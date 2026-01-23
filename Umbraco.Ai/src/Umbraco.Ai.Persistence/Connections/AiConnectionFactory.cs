using System.Text.Json;
using Umbraco.Ai.Core;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Persistence.Connections;

/// <summary>
/// Factory for mapping between <see cref="AiConnection"/> domain models and <see cref="AiConnectionEntity"/> database entities.
/// Handles encryption/decryption of sensitive settings fields during the mapping process.
/// </summary>
internal sealed class AiConnectionFactory : IAiConnectionFactory
{
    private readonly IAiEditableModelSerializer _serializer;
    private readonly AiProviderCollection _providers;

    public AiConnectionFactory(
        IAiEditableModelSerializer serializer,
        AiProviderCollection providers)
    {
        _serializer = serializer;
        _providers = providers;
    }

    /// <inheritdoc />
    public AiConnection BuildDomain(AiConnectionEntity entity)
    {
        object? settings = null;
        if (!string.IsNullOrEmpty(entity.Settings))
        {
            // Deserialize settings with automatic decryption of encrypted values
            settings = _serializer.Deserialize(entity.Settings);
        }

        var connection = new AiConnection
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            ProviderId = entity.ProviderId,
            Settings = settings,
            IsActive = entity.IsActive,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };

        // Set version using internal setter
        typeof(AiConnection).GetProperty(nameof(AiConnection.Version))!
            .SetValue(connection, entity.Version);

        return connection;
    }

    /// <inheritdoc />
    public AiConnectionEntity BuildEntity(AiConnection connection)
    {
        var schema = GetSchemaForProvider(connection.ProviderId);

        return new AiConnectionEntity
        {
            Id = connection.Id,
            Alias = connection.Alias,
            Name = connection.Name,
            ProviderId = connection.ProviderId,
            Settings = _serializer.Serialize(connection.Settings, schema),
            IsActive = connection.IsActive,
            Version = connection.Version,
            DateCreated = connection.DateCreated,
            DateModified = connection.DateModified,
            CreatedByUserId = connection.CreatedByUserId,
            ModifiedByUserId = connection.ModifiedByUserId
        };
    }

    /// <inheritdoc />
    public void UpdateEntity(AiConnectionEntity entity, AiConnection connection)
    {
        var schema = GetSchemaForProvider(connection.ProviderId);

        entity.Alias = connection.Alias;
        entity.Name = connection.Name;
        entity.ProviderId = connection.ProviderId;
        entity.Settings = _serializer.Serialize(connection.Settings, schema);
        entity.IsActive = connection.IsActive;
        entity.Version = connection.Version;
        entity.DateModified = connection.DateModified;
        entity.ModifiedByUserId = connection.ModifiedByUserId;
        // CreatedByUserId and DateCreated are intentionally not updated
    }

    /// <inheritdoc />
    public string CreateSnapshot(AiConnection connection)
    {
        // Create a snapshot with encrypted settings
        var schema = GetSchemaForProvider(connection.ProviderId);
        var encryptedSettings = _serializer.Serialize(connection.Settings, schema);

        var snapshot = new
        {
            connection.Id,
            connection.Alias,
            connection.Name,
            connection.ProviderId,
            Settings = encryptedSettings, // Encrypted JSON string
            connection.IsActive,
            connection.Version,
            connection.DateCreated,
            connection.DateModified,
            connection.CreatedByUserId,
            connection.ModifiedByUserId
        };

        return JsonSerializer.Serialize(snapshot, Constants.DefaultJsonSerializerOptions);
    }

    /// <inheritdoc />
    public AiConnection? BuildDomainFromSnapshot(string json)
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
                CreatedByUserId = root.TryGetProperty("createdByUserId", out var cbu) && cbu.ValueKind != JsonValueKind.Null
                    ? cbu.GetInt32() : null,
                ModifiedByUserId = root.TryGetProperty("modifiedByUserId", out var mbu) && mbu.ValueKind != JsonValueKind.Null
                    ? mbu.GetInt32() : null
            };
        }
        catch
        {
            return null;
        }
    }

    private AiEditableModelSchema? GetSchemaForProvider(string providerId)
    {
        var provider = _providers.GetById(providerId);
        return provider?.GetSettingsSchema();
    }
}
