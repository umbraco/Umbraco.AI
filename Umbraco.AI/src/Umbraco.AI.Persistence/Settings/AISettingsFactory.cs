using System.Reflection;
using System.Text.Json;
using Umbraco.AI.Core.Settings;
using Umbraco.Extensions;

namespace Umbraco.AI.Persistence.Settings;

/// <summary>
/// Factory for converting between <see cref="AISettingsEntity"/> and <see cref="AISettings"/>.
/// </summary>
internal static class AISettingsFactory
{
    private static readonly PropertyInfo[] SettingProperties = typeof(AISettings)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.GetCustomAttribute<AISettingAttribute>() != null)
        .ToArray();
    
    /// <summary>
    /// Builds a domain model from a collection of setting entities.
    /// </summary>
    public static AISettings BuildDomain(IEnumerable<AISettingsEntity> entities)
    {
        var settings = new AISettings();
        
        var entityDict = entities.ToDictionary(e => e.Key);
        
        foreach (var prop in SettingProperties)
        {
            var key = prop.GetCustomAttribute<AISettingAttribute>()?.Key ?? prop.Name;
            if (entityDict.TryGetValue(key, out var entity))
            {
                var value = ConvertFromString(entity.Value, prop.PropertyType);
                prop.SetValue(settings, value);
            }
        }
        
        var minEntity = entityDict.Values.MinBy(e => e.DateCreated);
        var maxEntity = entityDict.Values.MaxBy(e => e.DateModified);

        settings.DateCreated = minEntity?.DateCreated ?? DateTime.UtcNow;
        settings.CreatedByUserId = minEntity?.CreatedByUserId;
        settings.DateModified = maxEntity?.DateModified ?? DateTime.UtcNow;
        settings.ModifiedByUserId = maxEntity?.ModifiedByUserId;
        
        return settings;
    }

    /// <summary>
    /// Creates or updates entities from a domain model.
    /// </summary>
    public static IEnumerable<AISettingsEntity> BuildEntities(
        AISettings settings,
        IEnumerable<AISettingsEntity> existingEntities,
        Guid? userId)
    {
        var existing = existingEntities.ToDictionary(e => e.Key, e => e);
        var now = DateTime.UtcNow;

        foreach (var prop in SettingProperties)
        {
            var key = prop.GetCustomAttribute<AISettingAttribute>()?.Key ?? prop.Name;
            var value = ConvertToString(prop.GetValue(settings));
            yield return CreateOrUpdateEntity(key, value, existing, now, userId);
        }
    }

    private static AISettingsEntity CreateOrUpdateEntity(
        string key,
        string? value,
        Dictionary<string, AISettingsEntity> existing,
        DateTime now,
        Guid? userId)
    {
        if (existing.TryGetValue(key, out var entity))
        {
            if (entity.Value == value)
            {
                // No change
                return entity;
            }
            
            // Update existing
            entity.Value = value;
            entity.DateModified = now;
            entity.ModifiedByUserId = userId;
            return entity;
        }

        // Create new
        return new AISettingsEntity
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
    
    private static object? ConvertFromString(string? value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var attempt = value.TryConvertTo(targetType);
        if (attempt.Success)
            return attempt.Result;

        return value.DetectIsJson() 
            ? JsonSerializer.Deserialize(value, targetType, Core.Constants.DefaultJsonSerializerOptions) 
            : null;
    }

    private static string? ConvertToString(object? value)
    {
        if (value is null)
            return null;

        var type = value.GetType();
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        // Simple types that TryConvertTo can roundtrip
        if (underlying.IsPrimitive || underlying.IsEnum ||
            underlying == typeof(string) || underlying == typeof(decimal) ||
            underlying == typeof(Guid) || underlying == typeof(DateTime) ||
            underlying == typeof(DateTimeOffset) || underlying == typeof(TimeSpan) ||
            underlying == typeof(Version))
        {
            return value.TryConvertTo<string>().Result;
        }

        // Complex types need JSON
        return JsonSerializer.Serialize(value, Core.Constants.DefaultJsonSerializerOptions);
    }
}
