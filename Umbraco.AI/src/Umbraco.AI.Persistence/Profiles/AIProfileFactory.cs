using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Persistence.Profiles;

/// <summary>
/// Factory for mapping between <see cref="AIProfile"/> domain models and <see cref="AIProfileEntity"/> database entities.
/// </summary>
/// <remarks>
/// Capability-specific <see cref="AIProfile.Settings"/> continue to use the plain
/// <see cref="AIProfileSettingsSerializer"/>. Provider-declared <see cref="AIProfile.ProviderSettings"/>
/// go through the editable-model serializer with the provider's profile-settings schema so sensitive
/// fields are encrypted at rest — consistent with how connection settings are handled.
/// </remarks>
internal sealed class AIProfileFactory : IAIProfileFactory
{
    private readonly IAIEditableModelSerializer _serializer;
    private readonly AIProviderCollection _providers;

    public AIProfileFactory(IAIEditableModelSerializer serializer, AIProviderCollection providers)
    {
        _serializer = serializer;
        _providers = providers;
    }

    /// <inheritdoc />
    public AIProfile BuildDomain(AIProfileEntity entity)
    {
        IReadOnlyList<string> tags = Array.Empty<string>();
        if (!string.IsNullOrEmpty(entity.Tags))
        {
            tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var capability = (AICapability)entity.Capability;

        object? providerSettings = null;
        if (!string.IsNullOrEmpty(entity.ProviderSettings))
        {
            // Deserialize with automatic decryption of encrypted values (returns a JsonElement bag);
            // resolution/typing happens later at request time via IAIEditableModelResolver.
            providerSettings = _serializer.Deserialize(entity.ProviderSettings);
        }

        return new AIProfile
        {
            Id = entity.Id,
            Alias = entity.Alias,
            Name = entity.Name,
            Capability = capability,
            Model = new AIModelRef(entity.ProviderId, entity.ModelId),
            ConnectionId = entity.ConnectionId,
            Settings = AIProfileSettingsSerializer.Deserialize(capability, entity.Settings),
            ProviderSettings = providerSettings,
            Tags = tags,
            Version = entity.Version,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId
        };
    }

    /// <inheritdoc />
    public AIProfileEntity BuildEntity(AIProfile profile)
    {
        return new AIProfileEntity
        {
            Id = profile.Id,
            Alias = profile.Alias,
            Name = profile.Name,
            Capability = (int)profile.Capability,
            ProviderId = profile.Model.ProviderId,
            ModelId = profile.Model.ModelId,
            ConnectionId = profile.ConnectionId,
            Settings = AIProfileSettingsSerializer.Serialize(profile.Settings),
            ProviderSettings = _serializer.Serialize(profile.ProviderSettings, GetProfileSettingsSchema(profile)),
            Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null,
            Version = profile.Version,
            DateCreated = profile.DateCreated,
            DateModified = profile.DateModified,
            CreatedByUserId = profile.CreatedByUserId,
            ModifiedByUserId = profile.ModifiedByUserId
        };
    }

    /// <inheritdoc />
    public void UpdateEntity(AIProfileEntity entity, AIProfile profile)
    {
        entity.Alias = profile.Alias;
        entity.Name = profile.Name;
        entity.Capability = (int)profile.Capability;
        entity.ProviderId = profile.Model.ProviderId;
        entity.ModelId = profile.Model.ModelId;
        entity.ConnectionId = profile.ConnectionId;
        entity.Settings = AIProfileSettingsSerializer.Serialize(profile.Settings);
        entity.ProviderSettings = _serializer.Serialize(profile.ProviderSettings, GetProfileSettingsSchema(profile));
        entity.Tags = profile.Tags.Count > 0 ? string.Join(',', profile.Tags) : null;
        entity.Version = profile.Version;
        entity.DateModified = profile.DateModified;
        entity.ModifiedByUserId = profile.ModifiedByUserId;
        // DateCreated and CreatedByUserId are intentionally not updated
    }

    private AIEditableModelSchema? GetProfileSettingsSchema(AIProfile profile)
    {
        var provider = _providers.GetById(profile.Model.ProviderId);
        return provider?.GetProfileSettingsSchema(profile.Capability);
    }
}
