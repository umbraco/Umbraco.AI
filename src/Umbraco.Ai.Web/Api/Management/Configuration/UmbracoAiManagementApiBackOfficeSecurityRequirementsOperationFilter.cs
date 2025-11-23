using Umbraco.Cms.Api.Management.OpenApi;

namespace Umbraco.Ai.Cms.Api.Management.Api.Management.Configuration;

/// <summary>
/// Operation filter that applies back-office security requirements to the AI Management API.
/// </summary>
internal sealed class UmbracoAiManagementApiBackOfficeSecurityRequirementsOperationFilter : BackOfficeSecurityRequirementsOperationFilterBase
{
    /// <inheritdoc />
    protected override string ApiName => Constants.ManagementApi.ApiName;
}