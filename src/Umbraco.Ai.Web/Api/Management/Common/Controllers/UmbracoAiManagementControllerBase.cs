using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Filters;

namespace Umbraco.Ai.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI Management API controllers.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[MapToApi(Constants.ManagementApi.ApiName)]
[JsonOptionsName(Constants.ManagementApi.ApiName)]
[DisableBrowserCache]
[Produces("application/json")]
public abstract class UmbracoAiManagementControllerBase : ControllerBase
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
    /// Returns a 404 Not Found response for a connection.
    /// </summary>
    protected IActionResult ConnectionNotFound()
        => OperationStatusResult(ConnectionOperationStatus.NotFound, problemDetailsBuilder
            => NotFound(problemDetailsBuilder
                .WithTitle("Connection not found")
                .WithDetail("The specified connection could not be found.")
                .Build()));

    /// <summary>
    /// Returns a 404 Not Found response for a profile.
    /// </summary>
    protected IActionResult ProfileNotFound()
        => OperationStatusResult(ProfileOperationStatus.NotFound, problemDetailsBuilder
            => NotFound(problemDetailsBuilder
                .WithTitle("Profile not found")
                .WithDetail("The specified profile could not be found.")
                .Build()));

    /// <summary>
    /// Returns a 404 Not Found response for a provider.
    /// </summary>
    protected IActionResult ProviderNotFound()
        => OperationStatusResult(ProviderOperationStatus.NotFound, problemDetailsBuilder
            => NotFound(problemDetailsBuilder
                .WithTitle("Provider not found")
                .WithDetail("The specified provider could not be found.")
                .Build()));

    /// <summary>
    /// Returns a 400 Bad Request response for invalid settings.
    /// </summary>
    protected IActionResult InvalidSettings(string detail)
        => OperationStatusResult(ConnectionOperationStatus.InvalidSettings, problemDetailsBuilder
            => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid settings")
                .WithDetail(detail)
                .Build()));
}