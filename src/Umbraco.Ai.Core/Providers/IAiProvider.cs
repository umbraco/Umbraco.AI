using Umbraco.Ai.Core.Common;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// Base interface for all AI providers. Providers expose capabilities through capability-specific interfaces.
/// </summary>
public interface IAiProvider : IAiComponent
{
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
