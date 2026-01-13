using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Audit.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to get audits associated with a specific entity (e.g., content item).
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class ByEntityAuditController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByEntityAuditController"/> class.
    /// </summary>
    public ByEntityAuditController(IAiAuditService auditService, IUmbracoMapper umbracoMapper)
    {
        _auditService = auditService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get audits associated with a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID (e.g., content item GUID).</param>
    /// <param name="entityType">The entity type (e.g., "content", "media").</param>
    /// <param name="limit">Maximum number of audits to return (default: 10).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audits for the entity.</returns>
    [HttpGet("by-entity/{entityId}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AuditItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditsByEntity(
        [FromRoute] string entityId,
        [FromQuery] string entityType = "content",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var audits = await _auditService.GetEntityHistoryAsync(entityId, entityType, limit, cancellationToken);

        return Ok(_umbracoMapper.MapEnumerable<AiAudit, AuditItemResponseModel>(audits));
    }
}
