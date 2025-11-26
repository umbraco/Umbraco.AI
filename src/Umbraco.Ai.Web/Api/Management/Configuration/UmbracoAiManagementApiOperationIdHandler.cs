using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Web.Api;
using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Ai.Web.Api.Management.Configuration;

/// <summary>
/// Operation ID handler for the Umbraco AI Management API.
/// </summary>
internal sealed class UmbracoAiManagementApiOperationIdHandler : OperationIdHandler
{
    /// <inheritdoc />
    public UmbracoAiManagementApiOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
        : base(apiVersioningOptions)
    {
    }

    /// <inheritdoc />
    protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
        => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(Constants.ManagementApi.ApiNamespacePrefix) is true;
}