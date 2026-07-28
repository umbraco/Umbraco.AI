using System.Linq;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Core.EditableModels;

public static class AIEditableModelResolverExtensions
{
    /// <summary>
    /// Resolves settings for a provider without knowing the settings type at compile time.
    /// Uses the provider to determine the expected settings type and schema for validation.
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
            var schema = provider.GetSettingsSchema();

            // Invoke the generic ResolveModel<TModel> overload. Selecting the generic method
            // definition explicitly avoids an ambiguous match with the non-generic overload.
            var method = typeof(IAIEditableModelResolver)
                .GetMethods()
                .Single(m => m.Name == nameof(IAIEditableModelResolver.ResolveModel) && m.IsGenericMethodDefinition)
                .MakeGenericMethod(settingsType);

            return method.Invoke(resolver, [settings, schema]);
        }

        // Provider doesn't have settings, return null
        return null;
    }

    /// <summary>
    /// Resolves the provider-declared, profile-level settings (e.g. reasoning effort) for a given
    /// capability into a typed instance. Mirrors <see cref="ResolveSettingsForProvider"/> but reads the
    /// settings type/schema from the capability rather than the provider, so it can apply the same
    /// <c>$</c>-config resolution and validation the connection settings get.
    /// </summary>
    /// <param name="resolver">The resolver instance.</param>
    /// <param name="provider">The provider that owns the capability.</param>
    /// <param name="capability">The capability whose profile settings should be resolved.</param>
    /// <param name="capabilitySettings">The stored (untyped) capability-settings bag to resolve.</param>
    /// <returns>
    /// A typed capability-settings instance as <see cref="object"/>, or null if the bag was null or the
    /// capability declares no profile settings.
    /// </returns>
    public static object? ResolveCapabilitySettings(
        this IAIEditableModelResolver resolver,
        IAIProvider provider,
        AICapability capability,
        object? capabilitySettings)
    {
        if (capabilitySettings is null)
        {
            return null;
        }

        var capabilitySettingsType = provider
            .GetCapabilities()
            .FirstOrDefault(c => c.Kind == capability)?
            .CapabilitySettingsType;

        if (capabilitySettingsType is null)
        {
            return null;
        }

        var schema = provider.GetCapabilitySettingsSchema(capability);
        return resolver.ResolveModel(capabilitySettingsType, capabilitySettings, schema);
    }
}
