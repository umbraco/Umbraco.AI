using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umbraco.Ai.Web.Api.Common.Configuration;

/// <summary>
/// Swashbuckle operation filter that applies <see cref="SwaggerOperationAttribute"/> metadata to OpenAPI operations.
/// </summary>
internal sealed class SwaggerOperationFilter : IOperationFilter
{
    private readonly string _documentName;

    /// <summary>
    /// Initializes a new instance of the <see cref="SwaggerOperationFilter"/> class.
    /// </summary>
    /// <param name="documentName">The OpenAPI document name this filter applies to.</param>
    public SwaggerOperationFilter(string documentName)
    {
        _documentName = documentName;
    }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.DocumentName != _documentName)
        {
            return;
        }

        var attribute = context.MethodInfo.GetCustomAttribute<SwaggerOperationAttribute>();
        if (attribute is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(attribute.Id))
        {
            operation.OperationId = attribute.Id;
        }

        if (!string.IsNullOrWhiteSpace(attribute.Summary))
        {
            operation.Summary = attribute.Summary;
        }

        if (!string.IsNullOrWhiteSpace(attribute.Description))
        {
            operation.Description = attribute.Description;
        }
    }
}
