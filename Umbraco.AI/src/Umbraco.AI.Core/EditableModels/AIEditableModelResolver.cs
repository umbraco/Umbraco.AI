using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Service for resolving editable models from various storage formats.
/// </summary>
/// <remarks>
/// Configuration substitution (<c>$Key:Path</c>) is default-deny: a key is only resolved
/// when it falls under one of <see cref="AIOptions.AllowedConfigurationKeyPrefixes"/>, so a
/// settings author can only reference the configuration sections an administrator has opted
/// in — not arbitrary application configuration. A value that needs to start with a literal
/// <c>$</c> (rather than be treated as a reference) is written with a leading <c>$$</c>.
/// </remarks>
internal sealed class AIEditableModelResolver : IAIEditableModelResolver
{
    private readonly IConfiguration _configuration;
    private readonly IReadOnlyList<string> _allowedConfigKeyPrefixes;
    private readonly IReadOnlyList<string> _secretConfigKeyPrefixes;

    public AIEditableModelResolver(IConfiguration configuration, IOptions<AIOptions>? options = null)
    {
        _configuration = configuration;

        // Fall back to defaults (the Secrets/Variables allow-list) when constructed without
        // options. Production always supplies them via DI; this keeps the default secure
        // rather than permissive.
        var aiOptions = options?.Value ?? new AIOptions();
        _allowedConfigKeyPrefixes = aiOptions.AllowedConfigurationKeyPrefixes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
        _secretConfigKeyPrefixes = aiOptions.SecretConfigurationKeyPrefixes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
    }

    /// <inheritdoc />
    public TModel? ResolveModel<TModel>(object? data, AIEditableModelSchema? schema = null)
        where TModel : class, new()
        => (TModel?)ResolveModel(typeof(TModel), data, schema);

    /// <inheritdoc />
    public object? ResolveModel(Type modelType, object? data, AIEditableModelSchema? schema = null)
    {
        // If data is null, return null
        if (data is null)
        {
            return null;
        }

        object? deserialized;

        // Handle JsonElement deserialization
        if (data is JsonElement jsonElement)
        {
            deserialized = jsonElement.Deserialize(modelType, Constants.DefaultJsonSerializerOptions);
        }
        else if (modelType.IsInstanceOfType(data))
        {
            // Already correct type, clone via JSON round-trip to avoid mutating the original object,
            // then resolve configuration variables and validate on the copy
            var json = JsonSerializer.Serialize(data, Constants.DefaultJsonSerializerOptions);
            deserialized = JsonSerializer.Deserialize(json, modelType, Constants.DefaultJsonSerializerOptions);
        }
        else
        {
            // Try to serialize/deserialize through JSON as fallback
            try
            {
                var json = JsonSerializer.Serialize(data, Constants.DefaultJsonSerializerOptions);
                deserialized = JsonSerializer.Deserialize(json, modelType, Constants.DefaultJsonSerializerOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve model to type {modelType.Name}",
                    ex);
            }
        }

        if (deserialized is not null)
        {
            ResolveConfigurationVariablesInObject(deserialized);
            ValidateModel(deserialized, schema);
        }

        return deserialized;
    }

    private void ResolveConfigurationVariablesInObject(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;

            // Read the field's sensitivity from its attribute, so secret config keys can be
            // restricted to sensitive fields. A property with no field attribute is treated
            // as non-sensitive (the secure default).
            var isSensitiveField = property
                .GetCustomAttribute<AIEditableModelFieldAttribute>()?.IsSensitive ?? false;

            var value = property.GetValue(obj);
            var resolvedValue = ResolveConfigurationVariable(value, property.PropertyType, isSensitiveField);

            if (!Equals(value, resolvedValue))
            {
                property.SetValue(obj, resolvedValue);
            }
        }
    }

    private object? ResolveConfigurationVariable(object? value, Type targetType, bool isSensitiveField)
    {
        if (value is not string strValue)
        {
            return value;
        }

        // Escape hatch: a leading "$$" denotes a literal value that happens to start with
        // "$" (e.g. a guardrail regex or contains-pattern), not a configuration reference.
        // Strip one '$' and return the remainder verbatim — no allow-list or lookup applied.
        // Note this only concerns values that START with '$'; a trailing '$' (e.g. a regex
        // end-of-line anchor) is never treated as a reference and needs no escaping.
        if (AIConfigurationReference.IsEscapedLiteral(strValue))
        {
            return AIConfigurationReference.Unescape(strValue);
        }

        if (!AIConfigurationReference.IsReference(strValue))
        {
            return value;
        }

        // Extract configuration key
        var configKey = strValue.Substring(AIConfigurationReference.Prefix.Length);

        // Default-deny: only keys under an allowed prefix may be dereferenced. Checked
        // before the lookup so the rejection does not depend on whether the key exists.
        // See AIOptions.AllowedConfigurationKeyPrefixes.
        if (!MatchesPrefix(configKey, _allowedConfigKeyPrefixes))
        {
            throw new InvalidOperationException(
                $"Configuration key '{configKey}' is not permitted in settings. " +
                $"Only keys under an allowed prefix may be referenced with the $ syntax " +
                $"(by default '{string.Join("', '", _allowedConfigKeyPrefixes)}'). " +
                $"An administrator can place the value under an allowed section or extend " +
                $"Umbraco:AI:AllowedConfigurationKeyPrefixes in app settings.");
        }

        // Secret keys may only land in sensitive fields, so a resolved secret stays in a
        // field the system treats as credential-bearing. See SecretConfigurationKeyPrefixes.
        if (!isSensitiveField && MatchesPrefix(configKey, _secretConfigKeyPrefixes))
        {
            throw new InvalidOperationException(
                $"Configuration key '{configKey}' is a secret and may only be referenced from " +
                $"a sensitive field (one marked [AIField(IsSensitive = true)]). Move the value " +
                $"to a non-secret section (e.g. Umbraco:AI:Variables) if it is safe to expose " +
                $"in this field, or reference it from a sensitive field instead.");
        }

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

    /// <summary>
    /// Determines whether <paramref name="configKey"/> falls under one of <paramref name="prefixes"/>.
    /// Matching is segment-aware (a prefix matches the whole key or a key whose next character
    /// is the <c>:</c> section separator) and case-insensitive, so <c>Umbraco:AI:Secrets</c>
    /// permits <c>Umbraco:AI:Secrets:ApiKey</c> but not <c>Umbraco:AI:SecretsBackup:ApiKey</c>.
    /// </summary>
    private static bool MatchesPrefix(string configKey, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (!configKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (configKey.Length == prefix.Length || configKey[prefix.Length] == ':')
            {
                return true;
            }
        }

        return false;
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

    private void ValidateModel(object model, AIEditableModelSchema? schema)
    {
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
            var errorMessage = $"Validation failed for model '{schema.Type.Name}':\n" +
                               string.Join("\n", validationErrors);
            throw new InvalidOperationException(errorMessage);
        }
    }
}
