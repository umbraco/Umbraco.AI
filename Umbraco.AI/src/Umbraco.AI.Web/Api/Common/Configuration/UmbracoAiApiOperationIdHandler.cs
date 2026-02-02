using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Extensions;

namespace Umbraco.Ai.Web.Api.Common.Configuration;

/// <summary>
/// Operation ID handler for the Umbraco AI Management API.
/// Handles singular/plural naming based on whether the endpoint returns a single item or collection.
/// </summary>
public class UmbracoAiApiOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
    : OperationIdHandler(apiVersioningOptions)
{
    /// <summary>
    /// The namespace root for Umbraco AI API controllers.
    /// </summary>
    protected virtual string NameSpace => Constants.AppNamespaceRoot;

    /// <inheritdoc />
    protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
        => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(NameSpace) is true;

    /// <inheritdoc />
    public override string Handle(ApiDescription apiDescription)
        =>  $"{apiDescription.ActionDescriptor.RouteValues["action"]}".ToFirstLower();
}