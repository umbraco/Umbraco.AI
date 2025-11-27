using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Extensions;
using Umbraco.Cms.Api.Common.OpenApi;

namespace Umbraco.Ai.Web.Api.Management.Configuration;

/// <summary>
/// Operation ID handler for the Umbraco AI Management API.
/// Handles singular/plural naming based on whether the endpoint returns a single item or collection.
/// </summary>
internal sealed partial class UmbracoAiManagementApiOperationIdHandler : OperationIdHandler
{
    /// <inheritdoc />
    public UmbracoAiManagementApiOperationIdHandler(IOptions<ApiVersioningOptions> apiVersioningOptions)
        : base(apiVersioningOptions)
    { }

    /// <inheritdoc />
    protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
        => controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(Constants.ManagementApi.ApiNamespacePrefix) is true;

    /// <inheritdoc />
    public override string Handle(ApiDescription apiDescription)
    {
        var operationId = base.Handle(apiDescription);

        // Check if this is a single-item endpoint
        if (IsSingleItemEndpoint(apiDescription))
        {
            operationId = SingularizeFirstPluralWord(operationId);
        }

        return operationId;
    }

    private static bool IsSingleItemEndpoint(ApiDescription apiDescription)
    {
        var relativePath = apiDescription.RelativePath;
        var httpMethod = apiDescription.HttpMethod?.ToUpperInvariant();

        if (string.IsNullOrEmpty(relativePath))
        {
            return false;
        }

        // POST/PUT/DELETE to collection endpoints operate on single items
        // e.g., POST /connections creates one connection
        if (httpMethod is "POST" or "PUT" or "DELETE")
        {
            // Exclude sub-resource collections like POST /providers/{id}/models
            return !EndsWithSubCollection().IsMatch(relativePath);
        }

        // GET endpoints: single-item if they have route parameters
        // But exclude sub-resource collections (e.g., GET /providers/{id}/models returns multiple models)
        return HasRouteParameter().IsMatch(relativePath)
               && !EndsWithSubCollection().IsMatch(relativePath);
    }

    /// <summary>
    /// Singularizes the first plural word in an operation ID.
    /// E.g., "GetConnectionsById" -> "GetConnectionById"
    /// </summary>
    private static string SingularizeFirstPluralWord(string operationId)
    {
        // Match HTTP verb prefix followed by a capitalized plural word
        // Pattern: (Get|Post|Put|Delete|Patch)(Word ending in s)(Rest of string)
        var match = PluralWordPattern().Match(operationId);
        if (!match.Success)
        {
            return operationId;
        }

        var prefix = match.Groups[1].Value;      // e.g., "Get"
        var pluralWord = match.Groups[2].Value;  // e.g., "Connections"
        var suffix = match.Groups[3].Value;      // e.g., "ById"

        var singularWord = pluralWord.MakeSingularName();

        return $"{prefix}{singularWord}{suffix}";
    }

    // Matches: HttpVerb + PluralWord + Rest
    // e.g., "GetConnectionsById" captures ("Get", "Connections", "ById")
    [GeneratedRegex(@"^(Get|Post|Put|Delete|Patch)([A-Z][a-z]+s)(.*)$")]
    private static partial Regex PluralWordPattern();

    // Matches route parameters like {id}, {id:guid}, {alias}
    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex HasRouteParameter();

    // Matches paths ending with a sub-collection segment after a parameter
    // e.g., /providers/{id}/models (models is a collection)
    [GeneratedRegex(@"\{[^}]+\}/[^{/]+$")]
    private static partial Regex EndsWithSubCollection();
}