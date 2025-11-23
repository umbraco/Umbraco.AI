using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Core.Registry;

namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Service for resolving AI provider settings from various storage formats.
/// </summary>
internal sealed class AiSettingsResolver : IAiSettingsResolver
{
    private const string ConfigPrefix = "$";
    
    private readonly IAiRegistry _registry;
    private readonly IConfiguration _configuration;

    public AiSettingsResolver(IAiRegistry registry, IConfiguration configuration)
    {
        _registry = registry;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public TSettings? ResolveSettings<TSettings>(string providerId, object? settings)
        where TSettings : class, new()
    {
        // If settings is null, return null (or new instance if required by validation)
        if (settings is null)
        {
            return null;
        }

        // If already correct type, just resolve environment variables and validate
        if (settings is TSettings typedSettings)
        {
            ResolveConfigurationVariablesInObject(typedSettings);
            ValidateSettings(providerId, typedSettings);
            return typedSettings;
        }

        // Handle JsonElement deserialization
        if (settings is JsonElement jsonElement)
        {
            var deserialized = DeserializeFromJsonElement<TSettings>(jsonElement);
            if (deserialized is not null)
            {
                ResolveConfigurationVariablesInObject(deserialized);
                ValidateSettings(providerId, deserialized);
            }
            return deserialized;
        }

        // Try to serialize/deserialize through JSON as fallback
        try
        {
            var json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<TSettings>(json);
            if (deserialized is not null)
            {
                ResolveConfigurationVariablesInObject(deserialized);
                ValidateSettings(providerId, deserialized);
            }
            return deserialized;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to resolve settings for provider '{providerId}' to type {typeof(TSettings).Name}",
                ex);
        }
    }

    /// <inheritdoc />
    public object? ResolveSettingsForProvider(IAiProvider provider, object? settings)
    {
        if (settings is null)
        {
            return null;
        }

        // Get the provider's settings type
        var settingsType = provider.SettingsType;
        if (settingsType is not null)
        {
            // Use reflection to call ResolveSettings<TSettings>
            var method = GetType()
                .GetMethod(nameof(ResolveSettings))!
                .MakeGenericMethod(settingsType);

            return method.Invoke(this, [provider.Id, settings]);
        }

        // Provider doesn't have settings, return null
        return null;
    }

    private TSettings? DeserializeFromJsonElement<TSettings>(JsonElement jsonElement)
        where TSettings : class, new()
    {
        try
        {
            return JsonSerializer.Deserialize<TSettings>(jsonElement.GetRawText());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize JsonElement to {typeof(TSettings).Name}",
                ex);
        }
    }

    private void ResolveConfigurationVariablesInObject(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            var value = property.GetValue(obj);
            var resolvedValue = ResolveConfigurationVariable(value, property.PropertyType);

            if (!Equals(value, resolvedValue))
            {
                property.SetValue(obj, resolvedValue);
            }
        }
    }

    private object? ResolveConfigurationVariable(object? value, Type targetType)
    {
        // Only handle string values with the $config: prefix
        if (value is not string strValue || !strValue.StartsWith(ConfigPrefix))
        {
            return value;
        }

        // Extract configuration key
        var configKey = strValue.Substring(ConfigPrefix.Length);
        var configValue = _configuration[configKey];

        if (configValue is null)
        {
            throw new InvalidOperationException(
                $"Configuration key '{configKey}' not found. " +
                $"Ensure the key is set in appsettings.json, environment variables, or other configuration sources before using $config:{configKey} in connection settings.");
        }

        // Convert to target type if needed (supports string, int, bool, etc.)
        return ConvertToTargetType(configValue, targetType);
    }

    private object ConvertToTargetType(string value, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // String - return as-is
        if (underlyingType == typeof(string))
        {
            return value;
        }

        // Boolean
        if (underlyingType == typeof(bool))
        {
            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            throw new InvalidOperationException(
                $"Cannot convert environment variable value '{value}' to boolean.");
        }

        // Integer types
        if (underlyingType == typeof(int))
        {
            if (int.TryParse(value, out var intValue))
                return intValue;

            throw new InvalidOperationException(
                $"Cannot convert environment variable value '{value}' to integer.");
        }

        // Other numeric types
        if (underlyingType == typeof(long))
            return long.Parse(value);
        if (underlyingType == typeof(double))
            return double.Parse(value);
        if (underlyingType == typeof(decimal))
            return decimal.Parse(value);

        // Default: return as string
        return value;
    }

    private void ValidateSettings(string providerId, object settings)
    {
        var provider = _registry.GetProvider(providerId);
        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{providerId}' not found in registry.");
        }

        var settingDefinitions = provider.GetSettingDefinitions();
        var settingsType = settings.GetType();
        var validationErrors = new List<string>();

        foreach (var settingDef in settingDefinitions)
        {
            if (string.IsNullOrEmpty(settingDef.PropertyName))
                continue;

            var property = settingsType.GetProperty(settingDef.PropertyName);
            if (property is null)
                continue;

            var value = property.GetValue(settings);

            // Validate using each validation attribute
            foreach (var validationRule in settingDef.ValidationRules)
            {
                var validationContext = new ValidationContext(settings)
                {
                    MemberName = settingDef.PropertyName,
                    DisplayName = settingDef.Label
                };

                var validationResult = validationRule.GetValidationResult(value, validationContext);
                if (validationResult != ValidationResult.Success)
                {
                    validationErrors.Add(validationResult?.ErrorMessage ?? $"Validation failed for {settingDef.Label}");
                }
            }
        }

        if (validationErrors.Any())
        {
            var errorMessage = $"Validation failed for provider '{providerId}' settings:\n" +
                               string.Join("\n", validationErrors);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
