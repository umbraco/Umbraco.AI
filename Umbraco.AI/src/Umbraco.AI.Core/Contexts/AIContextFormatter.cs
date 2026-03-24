using System.Text;
using System.Text.Json;
using Umbraco.AI.Core.Contexts.ResourceTypes;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextFormatter"/>.
/// </summary>
internal sealed class AIContextFormatter : IAIContextFormatter
{
    private readonly AIContextResourceTypeCollection _resourceTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextFormatter"/> class.
    /// </summary>
    /// <param name="resourceTypes">The collection of resource types.</param>
    public AIContextFormatter(AIContextResourceTypeCollection resourceTypes)
    {
        _resourceTypes = resourceTypes;
    }

    /// <inheritdoc />
    public string FormatContextForLlm(AIResolvedContext context)
    {
        var hasInjected = context.InjectedResources.Count > 0;
        var hasOnDemand = context.OnDemandResources.Count > 0;

        if (!hasInjected && !hasOnDemand)
            return string.Empty;

        var sb = new StringBuilder();

        // Format injected (Always) resources
        if (hasInjected)
        {
            sb.AppendLine("## Context");
            sb.AppendLine();

            foreach (var resource in context.InjectedResources)
            {
                var formatted = FormatResourceForLlm(resource);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    sb.AppendLine($"### {resource.Name}");
                    sb.AppendLine(formatted);
                    sb.AppendLine();
                }
            }
        }

        // List on-demand resources so the LLM knows they're available
        if (hasOnDemand)
        {
            sb.AppendLine("## Available On-Demand Context Resources");
            sb.AppendLine();
            sb.AppendLine("The following context resources are available. Use the `get_context_resource` tool with the resource ID to retrieve the full content when needed:");
            sb.AppendLine();

            foreach (var resource in context.OnDemandResources)
            {
                sb.AppendLine($"- **{resource.Name}** (ID: `{resource.Id}`)");
                if (!string.IsNullOrWhiteSpace(resource.Description))
                {
                    sb.AppendLine($"  {resource.Description}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <inheritdoc />
    public string FormatResourceForLlm(AIResolvedResource resource)
    {
        var resourceType = _resourceTypes.GetById(resource.ResourceTypeId);
        if (resourceType is not null) return resourceType.FormatForLlm(resource.Data);
        
        // Fallback: return the data as JSON string if resource type not found
        if (resource.Data is null)
            return string.Empty;

        return resource.Data is JsonElement jsonElement
            ? jsonElement.ToString()
            : JsonSerializer.Serialize(resource.Data, Constants.DefaultJsonSerializerOptions);
    }
}
