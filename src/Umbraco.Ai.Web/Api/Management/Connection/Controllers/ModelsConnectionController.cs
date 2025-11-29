using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get available models for a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ModelsConnectionController : ConnectionControllerBase
{
    private readonly IAiConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsConnectionController"/> class.
    /// </summary>
    public ModelsConnectionController(
        IAiConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get available models for a connection.
    /// </summary>
    /// <param name="id">The unique identifier of the connection.</param>
    /// <param name="capability">Optional capability filter (Chat, Embedding, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    [HttpGet($"{{{nameof(id)}:guid}}/models")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ModelDescriptorResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelsByConnectionId(
        Guid id,
        [FromQuery] string? capability = null,
        CancellationToken cancellationToken = default)
    {
        var configured = await _connectionService.GetConfiguredProviderAsync(id, cancellationToken);
        if (configured is null)
        {
            return ConnectionNotFound();
        }

        // Get capabilities filtered by requested capability
        IEnumerable<IConfiguredCapability> capabilities = configured.GetCapabilities();
        if (!string.IsNullOrEmpty(capability) && Enum.TryParse<AiCapability>(capability, true, out var capFilter))
        {
            capabilities = capabilities.Where(c => c.Kind == capFilter);
        }

        // Fetch models from all matching capabilities
        var allModels = new List<AiModelDescriptor>();
        foreach (var cap in capabilities)
        {
            try
            {
                var models = await cap.GetModelsAsync(cancellationToken);
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
