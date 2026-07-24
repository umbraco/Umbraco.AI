using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Projects;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Controllers;

/// <summary>
/// Deletes a project.
/// </summary>
[ApiVersion("1.0")]
public class DeleteProjectController : ProjectControllerBase
{
    private readonly IAIProjectService _projectService;

    /// <summary>Initializes a new instance of the <see cref="DeleteProjectController"/> class.</summary>
    public DeleteProjectController(IAIProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Deletes one of the acting user's projects. Its resources cascade-delete; conversations in the
    /// project are orphaned (their project link is cleared), not deleted.
    /// </summary>
    /// <param name="id">The project id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content, or 404 if not found for the current user.</returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _projectService.DeleteProjectAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return ProjectNotFound();
        }

        return NoContent();
    }
}
