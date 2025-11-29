using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Settings;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Base interface for all AI providers. Providers expose capabilities through capability-specific interfaces.
/// </summary>
/// <remarks>
/// Providers are discovered via <see cref="IDiscoverable"/> and the <see cref="AiProviderAttribute"/>.
/// Use the <c>AiProviders()</c> collection builder extension method to add or exclude providers.
/// </remarks>
public interface IAiProvider : IDiscoverable
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
    /// Get the setting definitions that describe the configuration schema for this provider.
    /// Used by the UI to render connection configuration forms.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<AiSettingDefinition> GetSettingDefinitions();
    
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
    public bool TryGeCapability<TCapability>(out TCapability? capability) where TCapability : class, IAiCapability;

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
