using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Base interface for all AI providers. Providers expose capabilities through capability-specific interfaces.
/// </summary>
/// <remarks>
/// Providers are discovered via <see cref="IDiscoverable"/> and the <see cref="AIProviderAttribute"/>.
/// Use the <c>AIProviders()</c> collection builder extension method to add or exclude providers.
/// </remarks>
public interface IAIProvider : IDiscoverable
{
    /// <summary>
    /// The unique id of this AI component.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The name of this AI component.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type that represents the settings for this provider.
    /// </summary>
    Type? SettingsType { get; }

    /// <summary>
    /// Gets the settings schema that describes the configuration for this provider.
    /// Used by the UI to render connection configuration forms.
    /// </summary>
    /// <returns>The settings schema, or null if the provider has no settings.</returns>
    AIEditableModelSchema? GetSettingsSchema();

    /// <summary>
    /// Gets the schema describing the provider-declared capability settings for the given
    /// capability (e.g. reasoning effort). Used by the UI to render extra fields on the profile editor.
    /// </summary>
    /// <param name="capability">The capability whose capability-settings schema is requested.</param>
    /// <returns>The schema, or null if the capability is not supported or declares no capability settings.</returns>
    AIEditableModelSchema? GetCapabilitySettingsSchema(AICapability capability) => null;

    /// <summary>
    /// Gets all capabilities supported by this provider.
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<IAICapability> GetCapabilities();

    /// <summary>
    /// Tries to get the capability supported by this provider.
    /// </summary>
    /// <param name="capability"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public bool TryGetCapability<TCapability>(out TCapability? capability) where TCapability : class, IAICapability;

    /// <summary>
    /// Gets the capabilities supported by this provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public TCapability? GetCapability<TCapability>() where TCapability : class, IAICapability;

    /// <summary>
    /// Determines if the provider has a specific capability.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public bool HasCapability<TCapability>() where TCapability : class, IAICapability;

    /// <summary>
    /// Classifies an exception thrown by this provider's SDK into a normalised, user-safe
    /// <see cref="AIProviderErrorInfo"/>.
    /// </summary>
    /// <remarks>
    /// Called by the error-classifying client decorators in the capability factories, where the
    /// originating provider is known — so the exception is always one this provider produced.
    /// The base implementation handles the common transport types; providers override it to
    /// recognise SDK-specific error shapes.
    /// </remarks>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>The classified, user-safe error information.</returns>
    AIProviderErrorInfo ClassifyError(Exception exception);
}
