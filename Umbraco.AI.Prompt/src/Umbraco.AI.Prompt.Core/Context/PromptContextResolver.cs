using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Prompt.Core.Prompts;

namespace Umbraco.AI.Prompt.Core.Context;

/// <summary>
/// Resolves context from prompt-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the prompt ID from <see cref="Constants.MetadataKeys.PromptId"/> in the request properties,
/// then resolves any context IDs configured on the prompt.
/// </remarks>
internal sealed class PromptContextResolver : IAIContextResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIContextService _contextService;
    private readonly IAIPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor"></param>
    /// <param name="contextService">The context service.</param>
    /// <param name="promptService">The prompt service.</param>
    public PromptContextResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIContextService contextService,
        IAIPromptService promptService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contextService = contextService;
        _promptService = promptService;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var promptId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.MetadataKeys.PromptId);
        if (!promptId.HasValue)
        {
            return AIContextResolverResult.Empty;
        }

        var prompt = await _promptService.GetPromptAsync(promptId.Value, cancellationToken);
        if (prompt is null || prompt.ContextIds.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(prompt.ContextIds, prompt.Name, cancellationToken);
    }

    private async Task<AIContextResolverResult> ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string? entityName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AIContextResolverResource>();
        var sources = new List<AIContextResolverSource>();

        foreach (var contextId in contextIds)
        {
            var context = await _contextService.GetContextAsync(contextId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            sources.Add(new AIContextResolverSource(entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AIContextResolverResource
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

        return new AIContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
