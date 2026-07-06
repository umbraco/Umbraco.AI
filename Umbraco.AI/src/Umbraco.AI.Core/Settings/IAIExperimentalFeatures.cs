using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Single chokepoint for resolving whether an experimental AI feature is enabled.
/// </summary>
/// <remarks>
/// Non-experimental capabilities always report enabled. Experimental capabilities are gated
/// by their corresponding flag on <see cref="AIExperimentalOptions"/> (default off).
/// </remarks>
public interface IAIExperimentalFeatures
{
    /// <summary>
    /// Determines whether the given capability is available.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>
    /// <c>true</c> for non-experimental capabilities, or for experimental capabilities whose
    /// feature flag is enabled; otherwise <c>false</c>.
    /// </returns>
    bool IsCapabilityEnabled(AICapability capability);
}
