using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Provider.Controllers;

/// <summary>
/// Controller to get a provider by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdProviderController : ProviderControllerBase
{
    private readonly AiProviderCollection _providers;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdProviderController"/> class.
    /// </summary>
    public ByIdProviderController(AiProviderCollection providers, IUmbracoMapper umbracoMapper)
    {
        _providers = providers;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get a provider by its ID including settings schema.
    /// </summary>
    /// <param name="id">The unique identifier of the provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The provider details with settings schema.</returns>
    [HttpGet($"{{{nameof(id)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProviderResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetProviderById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var provider = _providers.GetById(id);
        if (provider is null)
        {
            return Task.FromResult(ProviderNotFound());
        }

        return Task.FromResult<IActionResult>(Ok(_umbracoMapper.Map<ProviderResponseModel>(provider)));
    }
}
