using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Ai.Web.Api.Common.Configuration;

/// <summary>
/// Operation filter to document Server-Sent Events (SSE) responses in OpenAPI.
/// </summary>
internal sealed class SseResponseOperationFilter : IOperationFilter
{
    private readonly string _documentName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerOperationFilter"/> class.
    /// </summary>
    /// <param name="documentName">The OpenAPI document name this filter applies to.</param>
    public SseResponseOperationFilter(string documentName)
    {
        _documentName = documentName;
    }
    
    /// <summary>
    /// Applies the filter to modify the OpenAPI operation.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.DocumentName != _documentName)
        {
            return;
        }
        
        // Check if the action produces text/event-stream
        var producesAttributes = context.MethodInfo
            .GetCustomAttributes(typeof(ProducesAttribute), true)
            .Cast<ProducesAttribute>();

        if (producesAttributes.Any(a => a.ContentTypes.Contains("text/event-stream")))
        {
            if (operation.Responses != null)
            {
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "Server-Sent Events stream",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["text/event-stream"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                        }
                    }
                };
            }
        }
    }
}