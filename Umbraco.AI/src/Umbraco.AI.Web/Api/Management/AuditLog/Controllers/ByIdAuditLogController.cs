using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Web.Api.Management.AuditLog.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Controller to get an audit-log by id.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class ByIdAuditLogController : AuditLogControllerBase
{
    private readonly IAIAuditLogService _auditLogService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdAuditLogController"/> class.
    /// </summary>
    public ByIdAuditLogController(IAIAuditLogService auditLogService, IUmbracoMapper umbracoMapper)
    {
        _auditLogService = auditLogService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get an audit-log by its id.
    /// </summary>
    /// <param name="auditLogId">The unique identifier of the audit-log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit-log details.</returns>
    [HttpGet($"{{{nameof(auditLogId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AuditLogDetailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogByIdentifier(
        [FromRoute] Guid auditLogId,
        CancellationToken cancellationToken = default)
    {
        // Get the audit-log with full details
        var audit = await _auditLogService.GetAuditLogAsync(auditLogId, cancellationToken);
        if (audit is null)
        {
            return AuditLogOperationStatusResult(AuditLogOperationStatus.NotFound);
        }

        return Ok(_umbracoMapper.Map<AuditLogDetailResponseModel>(audit));
    }
}
