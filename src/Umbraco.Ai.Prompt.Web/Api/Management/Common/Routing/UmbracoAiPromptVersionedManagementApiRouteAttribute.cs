using Umbraco.Cms.Web.Common.Routing;
using UmbracoAiConstants = Umbraco.Ai.Web.Api.Constants;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Common.Routing;

/// <summary>
/// Versioned route attribute for Umbraco AI Prompt Management API endpoints.
/// Routes are placed under the shared Umbraco.Ai API path.
/// </summary>
public class UmbracoAiPromptVersionedManagementApiRouteAttribute : BackOfficeRouteAttribute
{
    /// <summary>
    /// Creates a new versioned route attribute.
    /// </summary>
    /// <param name="template">The route template (without the base path).</param>
    public UmbracoAiPromptVersionedManagementApiRouteAttribute(string template)
        : base($"{UmbracoAiConstants.ManagementApi.BackofficePath}/v{{version:apiVersion}}/{template.TrimStart('/')}")
    {
    }
}
