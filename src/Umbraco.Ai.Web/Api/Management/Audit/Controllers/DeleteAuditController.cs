using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to delete an audit.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteAuditController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAuditController"/> class.
    /// </summary>
    public DeleteAuditController(IAiAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Delete an audit by its id.
    /// </summary>
    /// <param name="auditId">The unique identifier of the audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(auditId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAudit(
        [FromRoute] Guid auditId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _auditService.DeleteAuditAsync(auditId, cancellationToken);
        if (!deleted)
        {
            return AuditOperationStatusResult(AuditOperationStatus.NotFound);
        }

        return NoContent();
    }
}
