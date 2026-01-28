using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Profile.Controllers;

/// <summary>
/// Base controller for Profile management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Profile.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Profile.RouteSegment)]
public abstract class ProfileControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a profile operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult ProfileOperationStatusResult(ProfileOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            ProfileOperationStatus.NotFound => ProfileNotFound(),
            ProfileOperationStatus.DuplicateAlias => BadRequest(problemDetailsBuilder
                .WithTitle("Duplicate alias")
                .WithDetail("A profile with this alias already exists.")
                .Build()),
            ProfileOperationStatus.ConnectionNotFound => BadRequest(problemDetailsBuilder
                .WithTitle("Connection not found")
                .WithDetail("The specified connection does not exist.")
                .Build()),
            ProfileOperationStatus.ProviderNotFound => ProviderNotFound(),
            ProfileOperationStatus.InvalidModel => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid model")
                .WithDetail("The specified model is not valid for the provider.")
                .Build()),
            ProfileOperationStatus.InvalidCapability => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid capability")
                .WithDetail("The specified capability is not supported.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown profile operation status")
                .Build())
        });
}
