using System.Text.Json;
using Umbraco.AI.Core;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Persistence.Connections;

/// <summary>
/// Factory for mapping between <see cref="AIConnection"/> domain models and <see cref="AIConnectionEntity"/> database entities.
/// Handles encryption/decryption of sensitive settings fields during the mapping process.
/// </summary>
internal sealed class AIConnectionFactory : IAIConnectionFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIProviderCollection _providers;

    public AIConnectionFactory(
        IAIEditableModelSerializer serializer,
        AIProviderCollection providers)
    {
        _serializer = serializer;
        _providers = providers;
    }

    /// <inheritdoc />
    public AIConnection BuildDomain(AIConnectionEntity entity)
    {
        object? settings = null;
        if (!string.IsNullOrEmpty(entity.Settings))
        {
            // Deserialize settings with automatic decryption of encrypted values
            settings = _serializer.Deserialize(entity.Settings);
        }

        var connection = new AIConnection
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
        typeof(AIConnection).GetProperty(nameof(AIConnection.Version))!
            .SetValue(connection, entity.Version);

        return connection;
    }

    /// <inheritdoc />
    public AIConnectionEntity BuildEntity(AIConnection connection)
    {
        var schema = GetSchemaForProvider(connection.ProviderId);

        return new AIConnectionEntity
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
    public void UpdateEntity(AIConnectionEntity entity, AIConnection connection)
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

    private AIEditableModelSchema? GetSchemaForProvider(string providerId)
    {
        var provider = _providers.GetById(providerId);
        return provider?.GetSettingsSchema();
    }
}
