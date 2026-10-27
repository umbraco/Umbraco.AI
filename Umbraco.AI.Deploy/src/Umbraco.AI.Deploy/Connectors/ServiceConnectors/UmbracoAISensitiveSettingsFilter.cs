using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Decides which editable-model settings values may cross an environment boundary in a deploy artifact.
/// </summary>
/// <remarks>
/// The same rule applies on export and on import, so an artifact written before this rule existed
/// can't carry a secret into another environment either:
/// <list type="bullet">
///   <item>Fields in <c>ignoreSettings</c> are dropped, whatever their value.</item>
///   <item>Encrypted values (<c>ENC:...</c>) are dropped from any field. Each environment has its own
///   encryption keys, so they can't be read anywhere else.</item>
///   <item>Sensitive fields keep only configuration references (<c>$Section:Key</c>), unless listed in
///   <c>includeSensitiveSettings</c>. An escaped literal (<c>$$...</c>) is a literal, not a reference.</item>
/// </list>
/// Sensitivity comes from the schema. Without a schema, every string field is treated as sensitive.
/// </remarks>
internal static class UmbracoAISensitiveSettingsFilter
{
    private const string EncryptedPrefix = "ENC:";
    private const string ReferencePrefix = "$";
    private const string EscapedReferencePrefix = "$$";

    /// <summary>
    /// Converts a settings object of any shape (typed model, dictionary, <see cref="JsonElement"/>) to a
    /// dictionary of JSON values, or <c>null</c> when it isn't a JSON object.
    /// </summary>
    public static Dictionary<string, JsonElement>? ToDictionary(object? settings)
    {
        if (settings is null)
        {
            return null;
        }

        JsonElement element = settings is JsonElement json ? json : JsonSerializer.SerializeToElement(settings);
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            result[property.Name] = property.Value.Clone();
        }

        return result;
    }

    /// <summary>
    /// Returns the subset of <paramref name="settings"/> that may be written to, or read from, an artifact.
    /// </summary>
    public static Dictionary<string, JsonElement>? Filter(
        object? settings,
        AIEditableModelSchema? schema,
        IEnumerable<string> ignoreSettings,
        IEnumerable<string> includeSensitiveSettings)
    {
        Dictionary<string, JsonElement>? values = ToDictionary(settings);
        if (values is null)
        {
            return null;
        }

        var ignored = new HashSet<string>(ignoreSettings, StringComparer.OrdinalIgnoreCase);
        var includedSensitive = new HashSet<string>(includeSensitiveSettings, StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, JsonElement value) in values)
        {
            if (ignored.Contains(key))
            {
                continue;
            }

            string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            if (text?.StartsWith(EncryptedPrefix, StringComparison.Ordinal) == true)
            {
                continue;
            }

            if (IsSensitive(schema, key, value) && !IsConfigurationReference(text) && !includedSensitive.Contains(key))
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Removes sensitive literal values from a settings bag whose schema declares sensitive fields, for
    /// entities that have no per-field deploy options. Settings are returned unchanged when the schema
    /// declares no sensitive field (or is unknown), so their shape is preserved.
    /// </summary>
    public static object? RemoveSensitiveLiterals(object? settings, AIEditableModelSchema? schema)
    {
        if (settings is null || !GetSensitiveFields(schema).Any())
        {
            return settings;
        }

        Dictionary<string, JsonElement>? filtered = Filter(settings, schema, [], []);
        return filtered is null ? settings : JsonSerializer.SerializeToElement(filtered);
    }

    /// <summary>
    /// Fills sensitive fields that <paramref name="settings"/> leaves empty with the target environment's
    /// own value, so a deploy doesn't wipe a secret it was never allowed to carry.
    /// </summary>
    public static void KeepTargetSensitiveValues(
        Dictionary<string, JsonElement> settings,
        object? targetSettings,
        AIEditableModelSchema? schema)
    {
        Dictionary<string, JsonElement>? existing = ToDictionary(targetSettings);
        if (existing is null)
        {
            return;
        }

        foreach (AIEditableModelField field in GetSensitiveFields(schema))
        {
            if (settings.TryGetValue(field.Key, out JsonElement incoming) && !IsEmpty(incoming))
            {
                continue;
            }

            if (existing.TryGetValue(field.Key, out JsonElement current) && !IsEmpty(current))
            {
                settings[field.Key] = current;
            }
        }
    }

    /// <summary>
    /// Applies <see cref="RemoveSensitiveLiterals"/> to incoming settings, then
    /// <see cref="KeepTargetSensitiveValues"/>, for entities that have no per-field deploy options.
    /// </summary>
    public static object? ImportSettings(object? incomingSettings, object? targetSettings, AIEditableModelSchema? schema)
    {
        object? incoming = RemoveSensitiveLiterals(incomingSettings, schema);
        if (!GetSensitiveFields(schema).Any())
        {
            return incoming;
        }

        Dictionary<string, JsonElement> settings =
            ToDictionary(incoming) ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        KeepTargetSensitiveValues(settings, targetSettings, schema);
        return settings.Count == 0 && incomingSettings is null ? null : JsonSerializer.SerializeToElement(settings);
    }

    /// <summary>
    /// Gets the sensitive fields of <paramref name="schema"/>.
    /// </summary>
    public static IEnumerable<AIEditableModelField> GetSensitiveFields(AIEditableModelSchema? schema)
        => schema?.Fields.Where(f => f.IsSensitive) ?? [];

    /// <summary>
    /// Determines whether <paramref name="field"/> must have a value for the model to validate.
    /// </summary>
    public static bool IsRequired(AIEditableModelField field)
        => field.ValidationRules.OfType<RequiredAttribute>().Any();

    /// <summary>
    /// Determines whether a JSON value is absent in practice (null, or an empty string).
    /// </summary>
    public static bool IsEmpty(JsonElement value)
        => value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
           || (value.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(value.GetString()));

    private static bool IsSensitive(AIEditableModelSchema? schema, string key, JsonElement value)
    {
        if (schema is null)
        {
            // Without a schema we can't tell which field holds a secret, so treat every string as one.
            return value.ValueKind == JsonValueKind.String;
        }

        return schema.Fields.Any(f =>
            f.IsSensitive
            && (string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.PropertyName, key, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsConfigurationReference(string? value)
        => value is not null
           && value.StartsWith(ReferencePrefix, StringComparison.Ordinal)
           && !value.StartsWith(EscapedReferencePrefix, StringComparison.Ordinal);
}
