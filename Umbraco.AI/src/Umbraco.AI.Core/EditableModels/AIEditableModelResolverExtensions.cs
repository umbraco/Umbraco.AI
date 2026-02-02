using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Core.EditableModels;

public static class AIEditableModelResolverExtensions
{
    /// <summary>
    /// Resolves settings for a provider without knowing the settings type at compile time.
    /// Uses the provider to determine the expected settings type.
    /// </summary>
    /// <param name="resolver">The resolver instance</param>
    /// <param name="provider">The provider to resolve settings for.</param>
    /// <param name="settings">The settings object to resolve.</param>
    /// <returns>Typed settings instance as object, or null if settings parameter was null.</returns>
    public static object? ResolveSettingsForProvider(this IAIEditableModelResolver resolver, IAIProvider provider, object? settings)
    {
        if (settings is null)
        {
            return null;
        }

        // Get the provider's settings type
        var settingsType = provider.SettingsType;
        if (settingsType is not null)
        {
            // Use reflection to call ResolveModel<TModel>
            var method = resolver.GetType()
                .GetMethod(nameof(resolver.ResolveModel))!
                .MakeGenericMethod(settingsType);

            return method.Invoke(resolver, [provider.Id, settings]);
        }

        // Provider doesn't have settings, return null
        return null;
    }
}