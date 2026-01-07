using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api.Management.Common.OperationStatus;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.Ai.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI Management API controllers.
/// </summary>
[MapToApi(Constants.ManagementApi.ApiName)]
[JsonOptionsName(Constants.ManagementApi.ApiName)]
public abstract class UmbracoAiCoreManagementControllerBase : UmbracoAiManagementControllerBase
{
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

    /// <summary>
    /// Returns a 404 Not Found response for a context.
    /// </summary>
    protected IActionResult ContextNotFound()
        => OperationStatusResult(ContextOperationStatus.NotFound, problemDetailsBuilder
            => NotFound(problemDetailsBuilder
                .WithTitle("Context not found")
                .WithDetail("The specified context could not be found.")
                .Build()));
}