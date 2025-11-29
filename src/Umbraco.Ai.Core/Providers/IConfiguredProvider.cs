using Umbraco.Ai.Core.Connections;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// A provider with resolved settings. Mirrors IAiProvider API but
/// returns configured capabilities with settings baked in.
/// </summary>
public interface IConfiguredProvider
{
    /// <summary>
    /// The underlying provider.
    /// </summary>
    IAiProvider Provider { get; }

    /// <summary>
    /// The connection this configured provider was created from.
    /// </summary>
    AiConnection Connection { get; }

    /// <summary>
    /// Gets all configured capabilities.
    /// </summary>
    IReadOnlyList<IConfiguredCapability> GetCapabilities();

    /// <summary>
    /// Gets a specific configured capability by type.
    /// </summary>
    TCapability? GetCapability<TCapability>() where TCapability : class, IConfiguredCapability;

    /// <summary>
    /// Checks if the provider has a specific capability.
    /// </summary>
    bool HasCapability<TCapability>() where TCapability : class, IConfiguredCapability;
}
