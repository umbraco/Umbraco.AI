using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Service for resolving AI provider settings from various storage formats.
/// Handles JSON deserialization, environment variable substitution, and validation.
/// </summary>
public interface IAiSettingsResolver
{
    /// <summary>
    /// Resolves settings from storage format (JsonElement, Dictionary, etc.) to typed settings.
    /// Supports environment variable substitution using the pattern: $env:VARIABLE_NAME
    /// Validates the resolved settings using provider's validation rules.
    /// </summary>
    /// <typeparam name="TSettings">The type of settings to resolve to</typeparam>
    /// <param name="providerId">The provider ID for validation context</param>
    /// <param name="settings">The settings object to resolve (can be JsonElement, typed object, or null)</param>
    /// <returns>Typed settings instance, or null if settings parameter was null</returns>
    TSettings? ResolveSettings<TSettings>(string providerId, object? settings)
        where TSettings : class, new();

    /// <summary>
    /// Resolves settings for a provider without knowing the settings type at compile time.
    /// Uses the provider to determine the expected settings type.
    /// </summary>
    /// <param name="provider">The provider to resolve settings for</param>
    /// <param name="settings">The settings object to resolve</param>
    /// <returns>Typed settings instance as object, or null if settings parameter was null</returns>
    object? ResolveSettingsForProvider(IAiProvider provider, object? settings);
}
