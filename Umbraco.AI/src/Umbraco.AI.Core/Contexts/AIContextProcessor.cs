using System.Text;
using System.Text.Json;
using Umbraco.AI.Core.Contexts.ResourceTypes;

namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Default implementation of <see cref="IAIContextProcessor"/>.
/// </summary>
internal sealed class AIContextProcessor : IAIContextProcessor
{
    private readonly AIContextResourceTypeCollection _resourceTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextProcessor"/> class.
    /// </summary>
    /// <param name="resourceTypes">The collection of resource types.</param>
    public AIContextProcessor(AIContextResourceTypeCollection resourceTypes)
    {
        _resourceTypes = resourceTypes;
    }

    /// <inheritdoc />
    public async Task<string> ProcessContextForLlmAsync(AIResolvedContext context, CancellationToken cancellationToken = default)
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
                var formatted = await ProcessResourceForLlmAsync(resource, cancellationToken);
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
    public async Task<string> ProcessResourceForLlmAsync(AIResolvedResource resource, CancellationToken cancellationToken = default)
    {
        var resourceType = _resourceTypes.GetById(resource.ResourceTypeId);
        if (resourceType is not null)
        {
            var resolvedData = await resourceType.ResolveDataAsync(resource.Settings, cancellationToken);
            return resourceType.FormatDataForLlm(resolvedData);
        }

        // Fallback: return the settings as JSON string if resource type not found
        if (resource.Settings is null)
            return string.Empty;

        return resource.Settings is JsonElement jsonElement
            ? jsonElement.ToString()
            : JsonSerializer.Serialize(resource.Settings, Constants.DefaultJsonSerializerOptions);
    }
}
