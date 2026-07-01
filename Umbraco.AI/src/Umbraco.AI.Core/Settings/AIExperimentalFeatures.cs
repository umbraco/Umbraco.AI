using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Core.Settings;

/// <summary>
/// Default <see cref="IAIExperimentalFeatures"/> implementation backed by <see cref="AIExperimentalOptions"/>.
/// </summary>
internal sealed class AIExperimentalFeatures : IAIExperimentalFeatures
{
    private readonly IOptionsMonitor<AIExperimentalOptions> _options;

    public AIExperimentalFeatures(IOptionsMonitor<AIExperimentalOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public bool IsCapabilityEnabled(AICapability capability)
        => capability switch
        {
            // Experimental capabilities — gated by their feature flag (default off).
            AICapability.ImageGeneration => _options.CurrentValue.ImageGeneration,

            // Everything else is always enabled.
            _ => true,
        };
}
