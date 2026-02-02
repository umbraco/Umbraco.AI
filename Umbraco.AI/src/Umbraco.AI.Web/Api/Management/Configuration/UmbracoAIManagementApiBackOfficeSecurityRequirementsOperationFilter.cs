using Umbraco.AI.Web.Api;
using Umbraco.Cms.Api.Management.OpenApi;

namespace Umbraco.AI.Web.Api.Management.Configuration;

/// <summary>
/// Operation filter that applies back-office security requirements to the AI Management API.
/// </summary>
internal sealed class UmbracoAIManagementApiBackOfficeSecurityRequirementsOperationFilter(string apiName) : BackOfficeSecurityRequirementsOperationFilterBase
{
    /// <inheritdoc />
    protected override string ApiName => apiName;
}