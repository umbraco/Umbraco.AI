using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Governance;
using Umbraco.Ai.Web.Api.Common.Configuration;
using Umbraco.Ai.Web.Api.Management.Configuration;
using Umbraco.Ai.Web.Api.Management.Trace.Models;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Trace.Controllers;

/// <summary>
/// Controller to get all traces with filtering and pagination.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllTraceController : TraceControllerBase
{
    private readonly IAiTraceService _traceService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTraceController"/> class.
    /// </summary>
    public AllTraceController(IAiTraceService traceService, IUmbracoMapper umbracoMapper)
    {
        _traceService = traceService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get all traces with optional filtering and pagination.
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
    /// <returns>A paginated list of traces.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<TraceItemResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<TraceItemResponseModel>>> GetAllTraces(
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
        var filterRequest = new TraceFilterRequestModel
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

        var filter = _umbracoMapper.Map<AiTraceFilter>(filterRequest) ?? new AiTraceFilter();
        var (traces, total) = await _traceService.GetTracesPagedAsync(filter, skip, take, cancellationToken);

        var viewModel = new PagedViewModel<TraceItemResponseModel>
        {
            Total = total,
            Items = _umbracoMapper.MapEnumerable<AiTrace, TraceItemResponseModel>(traces)
        };

        return Ok(viewModel);
    }
}
