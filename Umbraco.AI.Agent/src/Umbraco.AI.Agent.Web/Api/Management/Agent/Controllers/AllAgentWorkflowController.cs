using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Management.Provider.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for retrieving all agent workflows.
/// </summary>
[ApiVersion("1.0")]
public class AllAgentWorkflowController : AgentControllerBase
{
    private readonly AIAgentWorkflowCollection _workflowCollection;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public AllAgentWorkflowController(AIAgentWorkflowCollection workflowCollection, IUmbracoMapper umbracoMapper)
    {
        _workflowCollection = workflowCollection;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all registered agent workflows.
    /// </summary>
    /// <returns>List of registered workflows with their settings schemas.</returns>
    [HttpGet("workflows")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AgentWorkflowItemResponseModel>), StatusCodes.Status200OK)]
    public IActionResult GetAgentWorkflows()
    {
        var models = _workflowCollection.Select(w => new AgentWorkflowItemResponseModel
        {
            Id = w.Id,
            Name = w.Name,
            Description = w.Description,
            SettingsSchema = w.SettingsType is not null
                ? _umbracoMapper.Map<EditableModelSchemaModel>(w.GetSettingsSchema())
                : null,
        });

        return Ok(models);
    }
}
