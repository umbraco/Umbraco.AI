using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Connection.Controllers;

/// <summary>
/// Base controller for Connection management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Connection.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Connection.RouteSegment)]
public abstract class ConnectionControllerBase : UmbracoAiCoreManagementControllerBase
{
    /// <summary>
    /// Maps a connection operation status to an appropriate action result.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <returns>The corresponding action result.</returns>
    protected IActionResult ConnectionOperationStatusResult(ConnectionOperationStatus status) =>
        OperationStatusResult(status, problemDetailsBuilder => status switch
        {
            ConnectionOperationStatus.NotFound => ConnectionNotFound(),
            ConnectionOperationStatus.ProviderNotFound => ProviderNotFound(),
            ConnectionOperationStatus.InvalidSettings => BadRequest(problemDetailsBuilder
                .WithTitle("Invalid connection settings")
                .WithDetail("The connection settings are invalid or incomplete.")
                .Build()),
            ConnectionOperationStatus.InUse => BadRequest(problemDetailsBuilder
                .WithTitle("Connection in use")
                .WithDetail("The connection cannot be deleted because it is in use by one or more profiles.")
                .Build()),
            ConnectionOperationStatus.TestFailed => BadRequest(problemDetailsBuilder
                .WithTitle("Connection test failed")
                .WithDetail("The connection could not be verified with the provider.")
                .Build()),
            _ => StatusCode(500, problemDetailsBuilder
                .WithTitle("Unknown connection operation status")
                .Build())
        });
}