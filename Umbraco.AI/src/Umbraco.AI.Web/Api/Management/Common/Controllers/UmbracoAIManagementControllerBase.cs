using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api;
using Umbraco.AI.Web.Api.Management.Common.OperationStatus;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Filters;

namespace Umbraco.AI.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI Management API controllers.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[DisableBrowserCache]
[Produces("application/json")]
public abstract class UmbracoAIManagementControllerBase : ControllerBase
{
    /// <summary>
    /// Creates an operation status result for the given status using a problem details builder.
    /// </summary>
    /// <typeparam name="TEnum">The operation status enum type.</typeparam>
    /// <param name="status">The operation status.</param>
    /// <param name="builder">A function that creates the action result using the problem details builder.</param>
    /// <returns>An IActionResult based on the operation status.</returns>
    protected static IActionResult OperationStatusResult<TEnum>(
        TEnum status,
        Func<ProblemDetailsBuilder, IActionResult> builder)
        where TEnum : Enum
        => builder(new ProblemDetailsBuilder().WithOperationStatus(status));

    /// <summary>
    /// Creates a problem details object with the given title and detail.
    /// </summary>
    /// <param name="title">The title of the problem.</param>
    /// <param name="detail">The detail message of the problem.</param>
    /// <returns>A ProblemDetails object.</returns>
    protected static ProblemDetails CreateProblemDetails(string title, string detail)
        => new ProblemDetailsBuilder()
            .WithTitle(title)
            .WithDetail(detail)
            .Build();
}