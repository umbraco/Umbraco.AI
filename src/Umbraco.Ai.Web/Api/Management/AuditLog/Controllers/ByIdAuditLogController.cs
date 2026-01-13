using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.AuditLog;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.AuditLog.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Controller to get an audit-log by id.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdAuditLogController : AuditLogControllerBase
{
    private readonly IAiAuditLogService _auditLogService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdAuditLogController"/> class.
    /// </summary>
    public ByIdAuditLogController(IAiAuditLogService auditLogService, IUmbracoMapper umbracoMapper)
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
