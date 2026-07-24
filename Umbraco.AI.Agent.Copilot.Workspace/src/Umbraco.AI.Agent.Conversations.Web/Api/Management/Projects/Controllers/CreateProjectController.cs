using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Creates a project.
/// </summary>
[ApiVersion("1.0")]
public class CreateProjectController : ProjectControllerBase
{
    private readonly IAIProjectService _projectService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="CreateProjectController"/> class.</summary>
    public CreateProjectController(IAIProjectService projectService, IUmbracoMapper umbracoMapper)
    {
        _projectService = projectService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Creates a project owned by the acting user.</summary>
    /// <param name="model">The project request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created project.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProjectResponseModel), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] ProjectRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var project = _umbracoMapper.Map<AIProject>(model)!;
        var created = await _projectService.SaveProjectAsync(project, cancellationToken);

        return CreatedAtAction(
            nameof(ByIdProjectController.GetById),
            "ByIdProject",
            new { id = created.Id },
            _umbracoMapper.Map<ProjectResponseModel>(created));
    }
}
