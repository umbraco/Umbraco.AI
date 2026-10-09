using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Capability.Controllers;

/// <summary>
/// Controller to get the AI capabilities enabled on this install.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class EnabledCapabilitiesController : CapabilityControllerBase
{
    /// <summary>
    /// Capabilities with a concrete <c>IAI*Capability</c> marker interface
    /// (see <see cref="Umbraco.AI.Core.Providers.IAICapability"/> and its siblings declared
    /// alongside it). Enumerated so <see cref="AICapability.Media"/> and
    /// <see cref="AICapability.Moderation"/> — reserved enum slots with no marker interface
    /// and no capability implementation anywhere in the codebase — are excluded, rather than
    /// surfaced as always-enabled placeholders (today's <see cref="IAIExperimentalFeatures"/>
    /// treats any capability it doesn't explicitly gate as enabled).
    /// </summary>
    internal static readonly IReadOnlySet<AICapability> ImplementedCapabilities = new HashSet<AICapability>
    {
        AICapability.Chat,
        AICapability.Embedding,
        AICapability.SpeechToText,
        AICapability.ImageGeneration,
        AICapability.Decision,
    };

    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnabledCapabilitiesController"/> class.
    /// </summary>
    public EnabledCapabilitiesController(IAIExperimentalFeatures experimentalFeatures)
    {
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Get the AI capabilities enabled on this install.
    /// </summary>
    /// <remarks>
    /// Returns every implemented <see cref="AICapability"/> whose feature flag is enabled, in
    /// enum declaration order. Non-experimental capabilities are always included. Reserved,
    /// unimplemented values (<see cref="AICapability.Media"/>, <see cref="AICapability.Moderation"/>)
    /// are never included.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of enabled capability names.</returns>
    [HttpGet("enabled")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<string>>> GetEnabledCapabilities(CancellationToken cancellationToken = default)
    {
        var enabled = Enum.GetValues<AICapability>()
            .Where(c => ImplementedCapabilities.Contains(c) && _experimentalFeatures.IsCapabilityEnabled(c))
            .Select(c => c.ToString());
        return Task.FromResult<ActionResult<IEnumerable<string>>>(Ok(enabled));
    }
}
