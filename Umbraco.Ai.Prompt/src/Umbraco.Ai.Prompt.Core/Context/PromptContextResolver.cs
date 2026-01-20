using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.Contexts.Resolvers;
using Umbraco.Ai.Prompt.Core.Prompts;

namespace Umbraco.Ai.Prompt.Core.Context;

/// <summary>
/// Resolves context from prompt-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the prompt ID from <see cref="Constants.MetadataKeys.PromptId"/> in the request properties,
/// then resolves any context IDs configured on the prompt.
/// </remarks>
internal sealed class PromptContextResolver : IAiContextResolver
{

    private readonly IAiContextService _contextService;
    private readonly IAiPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptContextResolver"/> class.
    /// </summary>
    /// <param name="contextService">The context service.</param>
    /// <param name="promptService">The prompt service.</param>
    public PromptContextResolver(
        IAiContextService contextService,
        IAiPromptService promptService)
    {
        _contextService = contextService;
        _promptService = promptService;
    }

    /// <inheritdoc />
    public async Task<AiContextResolverResult> ResolveAsync(
        AiContextResolverRequest request,
        CancellationToken cancellationToken = default)
    {
        var promptId = request.GetGuidProperty(Constants.MetadataKeys.PromptId);
        if (!promptId.HasValue)
        {
            return AiContextResolverResult.Empty;
        }

        var prompt = await _promptService.GetPromptAsync(promptId.Value, cancellationToken);
        if (prompt is null || prompt.ContextIds.Count == 0)
        {
            return AiContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(prompt.ContextIds, prompt.Name, cancellationToken);
    }

    private async Task<AiContextResolverResult> ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string? entityName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AiContextResolverResource>();
        var sources = new List<AiContextResolverSource>();

        foreach (var contextId in contextIds)
        {
            var context = await _contextService.GetContextAsync(contextId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            sources.Add(new AiContextResolverSource(entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AiContextResolverResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Data = resource.Data,
                    InjectionMode = resource.InjectionMode,
                    ContextName = context.Name
                });
            }
        }

        return new AiContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
