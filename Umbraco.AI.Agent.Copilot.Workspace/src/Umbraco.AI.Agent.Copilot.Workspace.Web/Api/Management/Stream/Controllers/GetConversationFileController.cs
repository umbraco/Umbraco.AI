using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream.Controllers;

/// <summary>
/// Serves a file uploaded within a persisted conversation. Unlike the shared, <c>[AllowAnonymous]</c>
/// agent file controller, this endpoint is authenticated (backoffice + Copilot Workspace section via the
/// base) and ownership-checked: the file is scoped to the conversation (<c>threadId := conversationId</c>)
/// and only served if the acting user owns that conversation. This closes the enumerable, unauthenticated
/// cross-user file-read hole that adopting durable conversation-scoped file ids would otherwise open (B6).
/// </summary>
[ApiVersion("1.0")]
public class GetConversationFileController : CopilotWorkspaceStreamControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IAIFileStore _fileStore;

    /// <summary>Initializes a new instance of the <see cref="GetConversationFileController"/> class.</summary>
    public GetConversationFileController(IAIConversationService conversationService, IAIFileStore fileStore)
    {
        _conversationService = conversationService;
        _fileStore = fileStore;
    }

    /// <summary>Gets a file stored within one of the acting user's conversations.</summary>
    /// <param name="id">The conversation id (also the file-store thread id).</param>
    /// <param name="fileId">The file id within the conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content, or 404 if the conversation or file is not found for the current user.</returns>
    [HttpGet("{id:guid}/files/{fileId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        Guid id,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        // Ownership check FIRST — never resolve a file for a conversation the caller doesn't own.
        // Not-found and not-owned are deliberately indistinguishable (can't probe ownership).
        var conversation = await _conversationService.GetConversationAsync(id, cancellationToken);
        if (conversation is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Conversation not found",
                Detail = "The specified conversation could not be found for the current user.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var storedFile = await _fileStore.ResolveAsync(id.ToString(), fileId, cancellationToken);
        if (storedFile is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "File not found",
                Detail = "The specified file could not be found in this conversation.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        return File(storedFile.Data, storedFile.MimeType, storedFile.Filename);
    }
}
