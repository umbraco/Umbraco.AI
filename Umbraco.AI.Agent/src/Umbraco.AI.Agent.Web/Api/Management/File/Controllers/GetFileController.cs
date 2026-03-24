using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Web.Api.Management.File.Controllers;

/// <summary>
/// Controller for serving files stored during agent conversations.
/// </summary>
[ApiVersion("1.0")]
public class GetFileController : FileControllerBase
{
    private readonly IAIFileStore _fileStore;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public GetFileController(IAIFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    /// <summary>
    /// Gets a stored file by thread ID and file ID.
    /// </summary>
    /// <param name="threadId">The conversation thread ID.</param>
    /// <param name="fileId">The file ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content with appropriate content type.</returns>
    [AllowAnonymous]
    [HttpGet($"{{{nameof(threadId)}}}/{{{nameof(fileId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        string threadId,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        var storedFile = await _fileStore.ResolveAsync(threadId, fileId, cancellationToken);
        if (storedFile is null)
        {
            return FileNotFound();
        }

        return File(storedFile.Data, storedFile.MimeType, storedFile.Filename);
    }
}
