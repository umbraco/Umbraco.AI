using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to clean up old audits based on retention policy.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CleanupAuditsController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupAuditsController"/> class.
    /// </summary>
    public CleanupAuditsController(IAiAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Trigger cleanup of old audits based on retention policy configured in settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of audits deleted.</returns>
    [HttpPost("cleanup")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupAudits(CancellationToken cancellationToken = default)
    {
        var deletedCount = await _auditService.CleanupOldAuditsAsync(cancellationToken);

        return Ok(deletedCount);
    }
}
