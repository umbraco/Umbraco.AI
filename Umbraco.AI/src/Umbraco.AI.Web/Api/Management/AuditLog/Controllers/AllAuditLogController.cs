using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Web.Api.Management.AuditLog.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.AuditLog.Controllers;

/// <summary>
/// Controller to get all audit-log logs with filtering and pagination.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllAuditLogController : AuditLogControllerBase
{
    private readonly IAIAuditLogService _auditLogService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllAuditLogController"/> class.
    /// </summary>
    public AllAuditLogController(IAIAuditLogService auditLogService, IUmbracoMapper umbracoMapper)
    {
        _auditLogService = auditLogService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all audit-log logs with optional filtering and pagination.
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
    [ProducesResponseType(typeof(PagedViewModel<AuditLogItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<AuditLogItemResponseModel>>> GetAuditLogs(
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
        var filterRequest = new AuditLogFilterRequestModel
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

        var filter = _umbracoMapper.Map<AIAuditLogFilter>(filterRequest) ?? new AIAuditLogFilter();
        var (auditLogs, total) = await _auditLogService.GetAuditLogsPagedAsync(filter, skip, take, cancellationToken);

        var viewModel = new PagedViewModel<AuditLogItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AIAuditLog, AuditLogItemResponseModel>(auditLogs)
        };

        return Ok(viewModel);
    }
}
