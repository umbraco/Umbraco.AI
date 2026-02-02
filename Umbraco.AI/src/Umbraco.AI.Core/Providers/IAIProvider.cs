using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
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
    /// Gets all capabilities supported by this provider.
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<IAiCapability> GetCapabilities();

    /// <summary>
    /// Tries to get the capability supported by this provider.
    /// </summary>
    /// <param name="capability"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public bool TryGetCapability<TCapability>(out TCapability? capability) where TCapability : class, IAiCapability;

    /// <summary>
    /// Gets the capabilities supported by this provider.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public TCapability? GetCapability<TCapability>() where TCapability : class, IAiCapability;

    /// <summary>
    /// Determines if the provider has a specific capability.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public bool HasCapability<TCapability>() where TCapability : class, IAiCapability;
}
