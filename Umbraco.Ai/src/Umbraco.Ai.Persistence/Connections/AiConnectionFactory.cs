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

    private AiEditableModelSchema? GetSchemaForProvider(string providerId)
    {
        var provider = _providers.GetById(providerId);
        return provider?.GetSettingsSchema();
    }
}
