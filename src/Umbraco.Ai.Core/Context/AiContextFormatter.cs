using System.Text;
using System.Text.Json;
using Umbraco.Ai.Core.Context.ResourceTypes;

namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Default implementation of <see cref="IAiContextFormatter"/>.
/// </summary>
internal sealed class AiContextFormatter : IAiContextFormatter
{
    private readonly AiContextResourceTypeCollection _resourceTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextFormatter"/> class.
    /// </summary>
    /// <param name="resourceTypes">The collection of resource types.</param>
    public AiContextFormatter(AiContextResourceTypeCollection resourceTypes)
    {
        _resourceTypes = resourceTypes;
    }

    /// <inheritdoc />
    public string FormatForSystemPrompt(AiResolvedContext context)
    {
        var injectedResources = context.InjectedResources;
        if (injectedResources.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("## Context");
        sb.AppendLine();

        foreach (var resource in injectedResources)
        {
            var formatted = FormatResource(resource);
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                sb.AppendLine($"### {resource.Name}");
                sb.AppendLine(formatted);
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc />
    public string FormatResource(AiResolvedResource resource)
    {
        var resourceType = _resourceTypes.GetById(resource.ResourceTypeId);
        if (resourceType is null)
        {
            // Fallback: return the data as JSON string if resource type not found
            if (resource.Data is null)
                return string.Empty;

            return resource.Data is JsonElement jsonElement
                ? jsonElement.ToString()
                : JsonSerializer.Serialize(resource.Data, Constants.DefaultJsonSerializerOptions);
        }

        return resourceType.FormatForInjection(resource.Data);
    }
}
