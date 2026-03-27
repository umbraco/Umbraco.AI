namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Resolves and formats context resources for injection into AI system prompts.
/// </summary>
public interface IAIContextProcessor
{
    /// <summary>
    /// Processes the entire resolved context into text suitable for AI consumption.
    /// Resolves resource data from settings and formats it for the LLM.
    /// </summary>
    /// <param name="context">The resolved context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted text for the context.</returns>
    Task<string> ProcessContextForLlmAsync(AIResolvedContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves and formats a single resource into text suitable for AI consumption.
    /// Resolves resource data from settings via the resource type, then formats it for the LLM.
    /// </summary>
    /// <param name="resource">The resolved resource.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted text for the resource.</returns>
    Task<string> ProcessResourceForLlmAsync(AIResolvedResource resource, CancellationToken cancellationToken = default);
}
