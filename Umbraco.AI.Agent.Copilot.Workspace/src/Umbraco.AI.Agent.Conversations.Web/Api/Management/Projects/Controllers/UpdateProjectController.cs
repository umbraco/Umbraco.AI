using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Updates a project.
/// </summary>
[ApiVersion("1.0")]
public class UpdateProjectController : ProjectControllerBase
{
    private readonly IAIProjectService _projectService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="UpdateProjectController"/> class.</summary>
    public UpdateProjectController(IAIProjectService projectService, IUmbracoMapper umbracoMapper)
    {
        _projectService = projectService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Updates one of the acting user's projects.</summary>
    /// <param name="id">The project id.</param>
    /// <param name="model">The project request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, or 404 if not found for the current user.</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ProjectRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var project = _umbracoMapper.Map<AIProject>(model)!;
        project.Id = id;

        try
        {
            await _projectService.SaveProjectAsync(project, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ProjectNotFound();
        }

        return NoContent();
    }
}
