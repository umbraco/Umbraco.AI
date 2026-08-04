using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Lists the acting user's projects (paged).
/// </summary>
[ApiVersion("1.0")]
public class AllProjectsController : ProjectControllerBase
{
    private readonly IAIProjectService _projectService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>Initializes a new instance of the <see cref="AllProjectsController"/> class.</summary>
    public AllProjectsController(IAIProjectService projectService, IUmbracoMapper umbracoMapper)
    {
        _projectService = projectService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>Gets a paged list of the acting user's projects, newest first.</summary>
    /// <param name="search">Optional search over project name.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list of projects.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<ProjectResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<ProjectResponseModel>>> GetAll(
        string? search = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _projectService.GetProjectsPagedAsync(skip, take, search, cancellationToken);

        return Ok(new PagedViewModel<ProjectResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AIProject, ProjectResponseModel>(items),
        });
    }
}
