using Umbraco.Ai.Web.Api.Common.Configuration;

namespace Umbraco.Ai.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco.Ai core package endpoints.
/// </summary>
/// <remarks>
/// All feature controllers in the Umbraco.Ai.Web project should inherit from this class
/// rather than <see cref="UmbracoAiManagementControllerBase"/> directly. This ensures
/// all core endpoints are tagged with "Umbraco Ai" for API client generation filtering.
/// </remarks>
[SwaggerOperation(Tags = ["UmbracoAiCore"])]
public abstract class UmbracoAiCoreManagementControllerBase : UmbracoAiManagementControllerBase
{ }
