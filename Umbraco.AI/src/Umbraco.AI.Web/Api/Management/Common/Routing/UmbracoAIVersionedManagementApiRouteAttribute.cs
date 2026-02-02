using Umbraco.AI.Web.Api;
using Umbraco.Cms.Web.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Common.Routing;

/// <summary>
/// Attribute for defining versioned Umbraco AI Management API routes.
/// </summary>
/// <param name="template">The route template.</param>
public class UmbracoAIVersionedManagementApiRouteAttribute(string template)
    : BackOfficeRouteAttribute($"{Constants.ManagementApi.BackofficePath}/v{{version:apiVersion}}/{template.TrimStart('/')}");