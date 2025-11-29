using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Provider.Controllers;

/// <summary>
/// Controller to get available models for a provider.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ModelsByProviderController : ProviderControllerBase
{
    private readonly AiProviderCollection _providers;
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsByProviderController"/> class.
    /// </summary>
    public ModelsByProviderController(AiProviderCollection providers, IAiConnectionService connectionService, IUmbracoMapper umbracoMapper)
    {
        _providers = providers;
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get available models for a provider using a specific connection.
    /// </summary>
    /// <param name="id">The unique identifier of the provider.</param>
    /// <param name="connectionId">The connection ID to use for fetching models.</param>
    /// <param name="capability">Optional capability filter (Chat, Embedding, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    [HttpGet($"{{{nameof(id)}}}/models")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ModelDescriptorResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelsByProviderId(
        string id,
        [FromQuery] Guid connectionId,
        [FromQuery] string? capability = null,
        CancellationToken cancellationToken = default)
    {
        var provider = _providers.GetById(id);
        if (provider is null)
        {
            return ProviderNotFound();
        }

        // Get connection to get settings
        var connection = await _connectionService.GetConnectionAsync(connectionId, cancellationToken);
        if (connection is null)
        {
            return ConnectionNotFound();
        }

        // Get capabilities filtered by requested capability
        var capabilities = provider.GetCapabilities();
        if (!string.IsNullOrEmpty(capability) && Enum.TryParse<AiCapability>(capability, true, out var capFilter))
        {
            capabilities = capabilities.Where(c => c.Kind == capFilter).ToList();
        }

        // Fetch models from all matching capabilities
        var allModels = new List<AiModelDescriptor>();
        foreach (var cap in capabilities)
        {
            try
            {
                var models = await cap.GetModelsAsync(connection.Settings, cancellationToken);
                allModels.AddRange(models);
            }
            catch
            {
                // If we can't fetch models for a capability, skip it
            }
        }

        // Remove duplicates and map to response
        var distinctModels = allModels
            .GroupBy(m => m.Model.ModelId)
            .Select(g => g.First());

        return Ok(_umbracoMapper.MapEnumerable<AiModelDescriptor, ModelDescriptorResponseModel>(distinctModels));
    }
}
