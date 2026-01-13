using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Audit;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Audit.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Audit.Controllers;

/// <summary>
/// Controller to get all audits with filtering and pagination.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllAuditController : AuditControllerBase
{
    private readonly IAiAuditService _auditService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllAuditController"/> class.
    /// </summary>
    public AllAuditController(IAiAuditService auditService, IUmbracoMapper umbracoMapper)
    {
        _auditService = auditService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all audits with optional filtering and pagination.
    /// </summary>
    /// <param name="status">Optional status filter (Running, Succeeded, Failed, etc.).</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="providerId">Optional provider ID filter.</param>
    /// <param name="entityId">Optional entity ID filter.</param>
    /// <param name="fromDate">Optional start date filter (ISO 8601 format).</param>
    /// <param name="toDate">Optional end date filter (ISO 8601 format).</param>
    /// <param name="searchText">Optional search text across multiple fields.</param>
    /// <param name="skip">Number of items to skip for pagination.</param>
    /// <param name="take">Number of items to take for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated list of audits.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<AuditItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<AuditItemResponseModel>>> GetAudits(
        string? status = null,
        string? userId = null,
        Guid? profileId = null,
        string? providerId = null,
        string? entityId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var filterRequest = new AuditFilterRequestModel
        {
            Status = status,
            UserId = userId,
            ProfileId = profileId,
            ProviderId = providerId,
            EntityId = entityId,
            FromDate = fromDate,
            ToDate = toDate,
            SearchText = searchText
        };

        var filter = _umbracoMapper.Map<AiAuditFilter>(filterRequest) ?? new AiAuditFilter();
        var (audits, total) = await _auditService.GetAuditsPagedAsync(filter, skip, take, cancellationToken);

        var viewModel = new PagedViewModel<AuditItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AiAudit, AuditItemResponseModel>(audits)
        };

        return Ok(viewModel);
    }
}
