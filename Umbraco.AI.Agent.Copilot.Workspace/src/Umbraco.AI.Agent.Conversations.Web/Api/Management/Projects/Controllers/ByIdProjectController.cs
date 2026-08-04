using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Gets a single project by id.
/// </summary>
[ApiVersion("1.0")]
public class ByIdProjectController : ProjectControllerBase
{
    private readonly IAIProjectService _projectService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="ByIdProjectController"/> class.</summary>
    public ByIdProjectController(IAIProjectService projectService, IUmbracoMapper umbracoMapper)
    {
        _projectService = projectService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets one of the acting user's projects by id.</summary>
    /// <param name="id">The project id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project, or 404 if not found for the current user.</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProjectResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectService.GetProjectAsync(id, cancellationToken);
        if (project is null)
        {
            return ProjectNotFound();
        }

        return Ok(_umbracoMapper.Map<ProjectResponseModel>(project));
    }
}
