using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Service for resolving editable models from various storage formats.
/// </summary>
internal sealed class AIEditableModelResolver : IAIEditableModelResolver
{
    private const string ConfigPrefix = "$";

    private readonly AIProviderCollection _providers;
    private readonly IConfiguration _configuration;

    public AIEditableModelResolver(AIProviderCollection providers, IConfiguration configuration)
    {
        _providers = providers;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public TModel? ResolveModel<TModel>(string modelId, object? data)
        where TModel : class, new()
    {
        // If data is null, return null (or new instance if required by validation)
        if (data is null)
        {
            return null;
        }

        // If already correct type, just resolve configuration variables and validate
        if (data is TModel typedModel)
        {
            ResolveConfigurationVariablesInObject(typedModel);
            ValidateModel(modelId, typedModel);
            return typedModel;
        }

        // Handle JsonElement deserialization
        if (data is JsonElement jsonElement)
        {
            var deserialized = jsonElement.Deserialize<TModel>(Constants.DefaultJsonSerializerOptions);
            if (deserialized is not null)
            {
                ResolveConfigurationVariablesInObject(deserialized);
                ValidateModel(modelId, deserialized);
            }
            return deserialized;
        }

        // Try to serialize/deserialize through JSON as fallback
        try
        {
            var json = JsonSerializer.Serialize(data, Constants.DefaultJsonSerializerOptions);
            var deserialized = JsonSerializer.Deserialize<TModel>(json, Constants.DefaultJsonSerializerOptions);
            if (deserialized is not null)
            {
                ResolveConfigurationVariablesInObject(deserialized);
                ValidateModel(modelId, deserialized);
            }
            return deserialized;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to resolve model '{modelId}' to type {typeof(TModel).Name}",
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
        // Only handle string values with the $ prefix
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
                $"Ensure the key is set in appsettings.json, environment variables, or other configuration sources before using ${configKey} in settings.");
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
                $"Cannot convert configuration value '{value}' to boolean.");
        }

        // Integer types
        if (underlyingType == typeof(int))
        {
            if (int.TryParse(value, out var intValue))
                return intValue;

            throw new InvalidOperationException(
                $"Cannot convert configuration value '{value}' to integer.");
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

    private void ValidateModel(string modelId, object model)
    {
        var provider = _providers.GetById(modelId);
        if (provider is null)
        {
            // Not a provider model, skip provider-specific validation
            return;
        }

        var schema = provider.GetSettingsSchema();
        if (schema is null)
        {
            return;
        }

        var modelType = model.GetType();
        var validationErrors = new List<string>();

        foreach (var field in schema.Fields)
        {
            if (string.IsNullOrEmpty(field.PropertyName))
                continue;

            var property = modelType.GetProperty(field.PropertyName);
            if (property is null)
                continue;

            var value = property.GetValue(model);

            // Validate using each validation attribute
            foreach (var validationRule in field.ValidationRules)
            {
                var validationContext = new ValidationContext(model)
                {
                    MemberName = field.PropertyName,
                    DisplayName = field.Label
                };

                var validationResult = validationRule.GetValidationResult(value, validationContext);
                if (validationResult != ValidationResult.Success)
                {
                    validationErrors.Add(validationResult?.ErrorMessage ?? $"Validation failed for {field.Label}");
                }
            }
        }

        if (validationErrors.Any())
        {
            var errorMessage = $"Validation failed for model '{modelId}':\n" +
                               string.Join("\n", validationErrors);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
