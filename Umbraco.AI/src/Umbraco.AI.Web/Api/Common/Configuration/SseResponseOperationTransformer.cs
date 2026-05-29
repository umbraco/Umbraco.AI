using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Documents endpoints that produce <c>text/event-stream</c> by replacing the default 200 response with
/// a single Server-Sent Events response. Detected via the controller method's <see cref="ProducesAttribute"/>.
/// </summary>
internal sealed class SseResponseOperationTransformer : IOpenApiOperationTransformer
{
    private const string EventStreamMediaType = "text/event-stream";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var producesEventStream = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<ProducesAttribute>()
            .Any(a => a.ContentTypes.Contains(EventStreamMediaType));

        if (producesEventStream == false)
        {
            return Task.CompletedTask;
        }

        operation.Responses ??= new OpenApiResponses();
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Server-Sent Events stream",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [EventStreamMediaType] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                },
            },
        };

        return Task.CompletedTask;
    }
}
