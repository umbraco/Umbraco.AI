using System.Text.RegularExpressions;
using Asp.Versioning;
using Humanizer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
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

        // Normalize the first word after the HTTP verb to singular or plural based on endpoint type
        var isSingleItem = IsSingleItemEndpoint(apiDescription);
        operationId = NormalizeFirstWord(operationId, isSingleItem);

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

        // POST/PUT/PATCH/DELETE to collection endpoints operate on single items
        // e.g., POST /connections creates one connection
        if (httpMethod is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            return true;
        }

        // GET endpoints: single-item if they have route parameters
        // But exclude sub-resource collections (e.g., GET /providers/{id}/models returns multiple models)
        return HasRouteParameter().IsMatch(relativePath);
    }

    /// <summary>
    /// Normalizes the first word after the HTTP verb to singular or plural.
    /// E.g., for single-item: "GetConnectionsById" -> "GetConnectionById"
    /// E.g., for collection: "GetConnection" -> "GetConnections"
    /// </summary>
    private static string NormalizeFirstWord(string operationId, bool toSingular)
    {
        // Match HTTP verb prefix followed by a capitalized word
        // Pattern: (Get|Post|Put|Delete|Patch)(CapitalizedWord)(Rest of string)
        var match = FirstWordPattern().Match(operationId);
        if (!match.Success)
        {
            return operationId;
        }

        var prefix = match.Groups[1].Value;  // e.g., "Get"
        var word = match.Groups[2].Value;    // e.g., "Connections" or "Connection"
        var suffix = match.Groups[3].Value;  // e.g., "ById"

        // Use Humanizer to convert, with inputIsKnownToBePlural/Singular: false
        // so it detects the current form and only converts if needed
        var normalizedWord = toSingular
            ? word.Singularize(inputIsKnownToBePlural: false)
            : word.Pluralize(inputIsKnownToBeSingular: false);

        return $"{prefix}{normalizedWord}{suffix}";
    }

    // Matches: HttpVerb + CapitalizedWord + Rest
    // e.g., "GetConnectionsById" captures ("Get", "Connections", "ById")
    [GeneratedRegex(@"^(Get|Post|Put|Delete|Patch)([A-Z][a-z]+[a-z]*)(.*)$")]
    private static partial Regex FirstWordPattern();

    // Matches route parameters like {id}, {id:guid}, {alias}
    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex HasRouteParameter();

    // // Matches paths ending with a sub-collection segment after a parameter
    // // e.g., /providers/{id}/models (models is a collection)
    // [GeneratedRegex(@"\{[^}]+\}/[^{/]+$")]
    // private static partial Regex EndsWithSubCollection();
}