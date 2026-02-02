using Umbraco.AI.Core.Connections;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// A provider with resolved settings. Mirrors IAiProvider API but
/// returns configured capabilities with settings baked in.
/// </summary>
public interface IAIConfiguredProvider
{
    /// <summary>
    /// The underlying provider.
    /// </summary>
    IAiProvider Provider { get; }

    /// <summary>
    /// Gets all configured capabilities.
    /// </summary>
    IReadOnlyList<IAiConfiguredCapability> GetCapabilities();

    /// <summary>
    /// Gets a specific configured capability by type.
    /// </summary>
    TCapability? GetCapability<TCapability>() where TCapability : class, IAiConfiguredCapability;

    /// <summary>
    /// Checks if the provider has a specific capability.
    /// </summary>
    bool HasCapability<TCapability>() where TCapability : class, IAiConfiguredCapability;
}
