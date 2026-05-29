using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Umbraco.Extensions;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Sets the OpenAPI operation ID to the controller action's name with the first letter lower-cased.
/// </summary>
/// <remarks>
/// Matches the operation ID convention used by Umbraco AI prior to the v18 OpenAPI migration so
/// downstream TypeScript clients retain the same method names without regeneration churn.
/// </remarks>
internal sealed class UmbracoAIOperationIdTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName) &&
            string.IsNullOrWhiteSpace(actionName) == false)
        {
            operation.OperationId = actionName.ToFirstLower();
        }

        return Task.CompletedTask;
    }
}
