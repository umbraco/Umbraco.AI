using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Controller to delete an audit-log.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class DeleteAuditLogController : AuditLogControllerBase
{
    private readonly IAiAuditLogService _auditLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAuditLogController"/> class.
    /// </summary>
    public DeleteAuditLogController(IAiAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Delete an audit-log by its id.
    /// </summary>
    /// <param name="auditLogId">The unique identifier of the audit-log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete($"{{{nameof(auditLogId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAuditLog(
        [FromRoute] Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _auditLogService.DeleteAuditLogAsync(auditLogId, cancellationToken);
        if (!deleted)
        {
            return AuditLogOperationStatusResult(AuditLogOperationStatus.NotFound);
        }

        return NoContent();
    }
}
