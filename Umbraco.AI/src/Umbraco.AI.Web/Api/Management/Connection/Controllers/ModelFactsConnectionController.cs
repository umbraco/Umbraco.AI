using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Connection.Mapping;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Controller to get the facts (context window, price, and so on) about a connection's models.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ModelFactsConnectionController : ConnectionControllerBase
{
    private readonly IAIConnectionService _connectionService;
    private readonly IAIModelFactService _modelFactService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly ILogger<ModelFactsConnectionController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelFactsConnectionController"/> class.
    /// </summary>
    public ModelFactsConnectionController(
        IAIConnectionService connectionService,
        IAIModelFactService modelFactService,
        IAIExperimentalFeatures experimentalFeatures,
        IUmbracoMapper umbracoMapper,
        ILogger<ModelFactsConnectionController> logger)
    {
        _connectionService = connectionService;
        _modelFactService = modelFactService;
        _experimentalFeatures = experimentalFeatures;
        _umbracoMapper = umbracoMapper;
        _logger = logger;
    }

    /// <summary>
    /// Get the facts about a connection's models for a capability.
    /// </summary>
    /// <param name="connectionIdOrAlias">The unique identifier or alias of the connection.</param>
    /// <param name="capability">The capability to list models for (Chat, Embedding, etc.). Required.</param>
    /// <param name="modelId">Optional model id. When given, only that model's facts are returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The models that have facts, each with its facts in display order.</returns>
    [HttpGet($"{{{nameof(connectionIdOrAlias)}}}/model-facts")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ModelFactsResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelFacts(
        IdOrAlias connectionIdOrAlias,
        [FromQuery, Required] string? capability,
        [FromQuery] string? modelId,
        CancellationToken cancellationToken = default)
    {
        // Enum.TryParse accepts numeric strings (even for defined members), so require a name.
        if (string.IsNullOrEmpty(capability)
            || char.IsDigit(capability[0])
            || capability[0] is '-' or '+'
            || !Enum.TryParse<AICapability>(capability, true, out var capabilityKind)
            || !Enum.IsDefined(capabilityKind))
        {
            return BadRequest(CreateProblemDetails(
                "Invalid capability",
                "A valid capability query parameter is required, for example 'Chat'."));
        }

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

        if (!_experimentalFeatures.IsCapabilityEnabled(capabilityKind))
        {
            return Ok(new ModelFactsResponseModel());
        }

        var configuredCapability = configured.GetCapabilities().FirstOrDefault(c => c.Kind == capabilityKind);
        if (configuredCapability is null)
        {
            return Ok(new ModelFactsResponseModel());
        }

        IReadOnlyList<AIModelDescriptor> models;
        try
        {
            var listed = await configuredCapability.GetModelsAsync(cancellationToken);
            models = string.IsNullOrEmpty(modelId)
                ? listed.ToList()
                : listed.Where(m => m.Model.ModelId == modelId).ToList();
            models = models
                .GroupBy(m => m.Model.ModelId, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Could not list {Capability} models for connection {ConnectionId}; returning no model facts.",
                capabilityKind,
                connectionId.Value);
            return Ok(new ModelFactsResponseModel());
        }

        if (models.Count == 0)
        {
            return Ok(new ModelFactsResponseModel());
        }

        var context = new AIModelFactContext
        {
            ConnectionId = connectionId.Value,
            ProviderId = configured.Provider.Id,
            Capability = capabilityKind,
        };

        var facts = await _modelFactService.GetModelFactsAsync(context, models, cancellationToken);

        var items = models
            .Where(m => facts.TryGetValue(m.Model.ModelId, out var modelFacts) && modelFacts.Count > 0)
            .Select(m => new ModelFactsItem(m.Model, facts[m.Model.ModelId]));

        return Ok(new ModelFactsResponseModel
        {
            Items = _umbracoMapper.MapEnumerable<ModelFactsItem, ModelFactsItemResponseModel>(items),
        });
    }
}
