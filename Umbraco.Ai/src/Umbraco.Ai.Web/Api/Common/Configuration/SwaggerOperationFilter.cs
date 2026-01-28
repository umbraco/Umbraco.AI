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

        // Get attribute from method first, then fall back to declaring class (with inheritance)
        var methodAttribute = context.MethodInfo.GetCustomAttribute<SwaggerOperationAttribute>();
        var classAttribute = context.MethodInfo.DeclaringType?.GetCustomAttribute<SwaggerOperationAttribute>(inherit: true);

        // Apply method-level properties (Id, Summary, Description)
        if (methodAttribute is not null)
        {
            if (!string.IsNullOrWhiteSpace(methodAttribute.Id))
            {
                operation.OperationId = methodAttribute.Id;
            }

            if (!string.IsNullOrWhiteSpace(methodAttribute.Summary))
            {
                operation.Summary = methodAttribute.Summary;
            }

            if (!string.IsNullOrWhiteSpace(methodAttribute.Description))
            {
                operation.Description = methodAttribute.Description;
            }
        }

        // Collect tags from both class and method (class tags first, then method tags)
        var tags = new List<string>();

        if (classAttribute?.Tags is { Length: > 0 })
        {
            tags.AddRange(classAttribute.Tags);
        }

        if (methodAttribute?.Tags is { Length: > 0 })
        {
            tags.AddRange(methodAttribute.Tags);
        }

        // Add unique tags to the operation
        operation.Tags ??= new HashSet<OpenApiTagReference>();
        foreach (var tag in tags.Distinct())
        {
            if (operation.Tags.All(t => t.Name != tag))
            {
                operation.Tags.Add(new OpenApiTagReference(tag));
            }
        }
    }
}
