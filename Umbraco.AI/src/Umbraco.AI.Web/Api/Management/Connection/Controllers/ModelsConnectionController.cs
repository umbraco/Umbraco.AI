using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get available models for a connection.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ModelsConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelsConnectionController"/> class.
    /// </summary>
    public ModelsConnectionController(
        IAIConnectionService connectionService,
        IUmbracoMapper umbracoMapper)
    {
        _connectionService = connectionService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get available models for a connection.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection.</param>
    /// <param name="capability">Optional capability filter (Chat, Embedding, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of available models.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/models")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<ModelDescriptorResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModels(
        IdOrAlias connectionIdOrAlias,
        [FromQuery] string? capability = null,
        CancellationToken cancellationToken = default)
    {
        var connectionId = await _connectionService.TryGetConnectionIdAsync(connectionIdOrAlias, cancellationToken);
        if (connectionId is null)
        {
            return ConnectionNotFound();
        }

        var configured = await _connectionService.GetConfiguredProviderAsync(connectionId.Value, cancellationToken);
        if (configured is null)
        {
            return ConnectionNotFound();
        }

        // Get capabilities filtered by requested capability
        IEnumerable<IAIConfiguredCapability> capabilities = configured.GetCapabilities();
        if (!string.IsNullOrEmpty(capability) && Enum.TryParse<AICapability>(capability, true, out var capFilter))
        {
            capabilities = capabilities.Where(c => c.Kind == capFilter);
        }

        // Fetch models from all matching capabilities
        var allModels = new List<AIModelDescriptor>();
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

        return Ok(_umbracoMapper.MapEnumerable<AIModelDescriptor, ModelDescriptorResponseModel>(distinctModels));
    }
}
