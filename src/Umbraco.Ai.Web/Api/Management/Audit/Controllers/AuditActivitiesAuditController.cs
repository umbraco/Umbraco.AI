using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Management.Audit.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to get audit activities for a audit.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AuditActivitiesAuditController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditActivitiesAuditController"/> class.
    /// </summary>
    public AuditActivitiesAuditController(IAiAuditService auditService, IUmbracoMapper umbracoMapper)
    {
        _auditService = auditService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get audit activities for an audit.
    /// </summary>
    /// <param name="auditId">The unique id of the audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit activities for the audit.</returns>
    [HttpGet($"{{{nameof(auditId)}}}/activities")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AuditActivityResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditActivities(
        [FromRoute] Guid auditId,
        CancellationToken cancellationToken = default)
    {
        // Get the audit activities
        var spans = await _auditService.GetAuditActivitiesAsync(auditId, cancellationToken);

        return Ok(_umbracoMapper.MapEnumerable<AiAuditActivity, AuditActivityResponseModel>(spans));
    }
}
