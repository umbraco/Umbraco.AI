using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Management.Configuration;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Provider.Controllers;

/// <summary>
/// Controller to get all registered providers.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllProviderController : ProviderControllerBase
{
    private readonly AIProviderCollection _providers;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllProviderController"/> class.
    /// </summary>
    public AllProviderController(AIProviderCollection providers, IUmbracoMapper umbracoMapper)
    {
        _providers = providers;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all registered providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all registered providers.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ProviderItemResponseModel>), StatusCodes.Status200OK)]
    public Task<ActionResult<IEnumerable<ProviderItemResponseModel>>> GetAllProviders(
        CancellationToken cancellationToken = default)
    {
        var providers = _umbracoMapper.MapEnumerable<IAIProvider, ProviderItemResponseModel>(_providers
            .OrderBy(x => x.Name));
        return Task.FromResult<ActionResult<IEnumerable<ProviderItemResponseModel>>>(Ok(providers));
    }
}
