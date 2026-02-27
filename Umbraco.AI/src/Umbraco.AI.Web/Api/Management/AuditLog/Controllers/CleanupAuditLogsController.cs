using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Controller to clean up old audit-log logs based on retention policy.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class CleanupAuditLogsController : AuditLogControllerBase
{
    private readonly IAIAuditLogService _auditLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupAuditLogsController"/> class.
    /// </summary>
    public CleanupAuditLogsController(IAIAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Trigger cleanup of old audit-log logs based on retention policy configured in settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of audit-log logs deleted.</returns>
    [HttpPost("cleanup")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupAuditLogs(CancellationToken cancellationToken = default)
    {
        var deletedCount = await _auditLogService.CleanupOldAuditLogsAsync(cancellationToken);

        return Ok(deletedCount);
    }
}
