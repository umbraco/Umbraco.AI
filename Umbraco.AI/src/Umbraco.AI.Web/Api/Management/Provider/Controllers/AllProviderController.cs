using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Providers;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Provider.Controllers;

/// <summary>
/// Controller to get all registered providers.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllProviderController : ProviderControllerBase
{
    private readonly AIProviderCollection _providers;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllProviderController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIExperimentalFeatures so that providers with every "
        + "capability disabled can be omitted. Will be removed in v19.")]
    public AllProviderController(AIProviderCollection providers, IUmbracoMapper umbracoMapper)
        : this(
            providers,
            umbracoMapper,
            StaticServiceProvider.Instance.GetRequiredService<IAIExperimentalFeatures>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllProviderController"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overload around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public AllProviderController(
        AIProviderCollection providers,
        IUmbracoMapper umbracoMapper,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _providers = providers;
        _umbracoMapper = umbracoMapper;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Get all registered providers.
    /// </summary>
    /// <remarks>
    /// Omits providers whose every capability is currently disabled (e.g. an
    /// experimental-only provider while its feature flag is off), since they'd otherwise
    /// list with nothing to do. A provider with at least one enabled capability is
    /// unaffected — its capability list is already filtered by
    /// <see cref="Mapping.ProviderMapDefinition"/>.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered providers with at least one enabled capability.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ProviderItemResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ProviderItemResponseModel>>> GetAllProviders(
        CancellationToken cancellationToken = default)
    {
        var visibleProviders = _providers
            .Where(p => p.GetCapabilities().Any(c => _experimentalFeatures.IsCapabilityEnabled(c.Kind)))
            .OrderBy(x => x.Name);
        var providers = _umbracoMapper.MapEnumerable<IAIProvider, ProviderItemResponseModel>(visibleProviders);
        return Task.FromResult<ActionResult<IEnumerable<ProviderItemResponseModel>>>(Ok(providers));
    }
}
