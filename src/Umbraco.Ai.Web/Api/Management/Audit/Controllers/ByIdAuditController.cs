using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Audit.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to get an audit by id.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByIdAuditController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdAuditController"/> class.
    /// </summary>
    public ByIdAuditController(IAiAuditService auditService, IUmbracoMapper umbracoMapper)
    {
        _auditService = auditService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get an audit by its id.
    /// </summary>
    /// <param name="auditId">The unique identifier of the audit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The audit details.</returns>
    [HttpGet($"{{{nameof(auditId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(AuditDetailResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditByIdentifier(
        [FromRoute] Guid auditId,
        CancellationToken cancellationToken = default)
    {
        // Get the audit with full details
        var audit = await _auditService.GetAuditAsync(auditId, cancellationToken, includeActivities: false);
        if (audit is null)
        {
            return AuditOperationStatusResult(AuditOperationStatus.NotFound);
        }

        return Ok(_umbracoMapper.Map<AuditDetailResponseModel>(audit));
    }
}
