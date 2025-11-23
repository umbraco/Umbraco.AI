using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Core.Registry;

/// <summary>
/// Defines a registry for AI providers within the Umbraco AI ecosystem.
/// Providers are discovered via assembly scanning and accessed by alias or capability.
/// </summary>
public interface IAiRegistry
{
    /// <summary>
    /// Gets all registered AI providers.
    /// </summary>
    IEnumerable<IAiProvider> Providers { get; }
    
    /// <summary>
    /// Gets all providers that support the specified capability.
    /// </summary>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public IEnumerable<IAiProvider> GetProvidersWithCapability<TCapability>()
        where TCapability : class, IAiCapability;

    /// <summary>
    /// Gets a specific provider by its alias (case-insensitive).
    /// </summary>
    /// <param name="alias">The unique alias of the provider.</param>
    /// <returns>The provider, or null if not found.</returns>
    IAiProvider? GetProvider(string alias);

    /// <summary>
    /// Gets a specific capability from a provider by its ID.
    /// </summary>
    /// <param name="providerId"></param>
    /// <typeparam name="TCapability"></typeparam>
    /// <returns></returns>
    public TCapability? GetCapability<TCapability>(string providerId)
        where TCapability : class, IAiCapability;
}
