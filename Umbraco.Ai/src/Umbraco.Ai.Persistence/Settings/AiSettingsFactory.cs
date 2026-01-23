using Umbraco.Ai.Core.Settings;

namespace Umbraco.Ai.Persistence.Settings;

/// <summary>
/// Factory for converting between <see cref="AiSettingsEntity"/> and <see cref="AiSettings"/>.
/// </summary>
internal static class AiSettingsFactory
{
    /// <summary>
    /// Well-known setting keys.
    /// </summary>
    public static class Keys
    {
        public const string DefaultChatProfileId = "DefaultChatProfileId";
        public const string DefaultEmbeddingProfileId = "DefaultEmbeddingProfileId";
    }

    /// <summary>
    /// Builds a domain model from a collection of setting entities.
    /// </summary>
    public static AiSettings BuildDomain(IEnumerable<AiSettingsEntity> entities)
    {
        var settings = new AiSettings();

        foreach (var entity in entities)
        {
            switch (entity.Key)
            {
                case Keys.DefaultChatProfileId:
                    settings.DefaultChatProfileId = TryParseGuid(entity.Value);
                    break;
                case Keys.DefaultEmbeddingProfileId:
                    settings.DefaultEmbeddingProfileId = TryParseGuid(entity.Value);
                    break;
            }
        }

        return settings;
    }

    /// <summary>
    /// Creates or updates entities from a domain model.
    /// </summary>
    public static IEnumerable<AiSettingsEntity> BuildEntities(
        AiSettings settings,
        IEnumerable<AiSettingsEntity> existingEntities,
        int? userId)
    {
        var existing = existingEntities.ToDictionary(e => e.Key, e => e);
        var now = DateTime.UtcNow;

        yield return CreateOrUpdateEntity(
            Keys.DefaultChatProfileId,
            settings.DefaultChatProfileId?.ToString(),
            existing,
            now,
            userId);

        yield return CreateOrUpdateEntity(
            Keys.DefaultEmbeddingProfileId,
            settings.DefaultEmbeddingProfileId?.ToString(),
            existing,
            now,
            userId);
    }

    private static AiSettingsEntity CreateOrUpdateEntity(
        string key,
        string? value,
        Dictionary<string, AiSettingsEntity> existing,
        DateTime now,
        int? userId)
    {
        if (existing.TryGetValue(key, out var entity))
        {
            // Update existing
            entity.Value = value;
            entity.DateModified = now;
            entity.ModifiedByUserId = userId;
            return entity;
        }

        // Create new
        return new AiSettingsEntity
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            DateCreated = now,
            DateModified = now,
            CreatedByUserId = userId,
            ModifiedByUserId = userId
        };
    }

    private static Guid? TryParseGuid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
