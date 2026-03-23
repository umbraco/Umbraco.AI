namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Formats resolved context resources for injection into AI system prompts.
/// </summary>
public interface IAIContextFormatter
{
    /// <summary>
    /// Formats the entire resolved context into text suitable for AI consumption.
    /// </summary>
    /// <param name="context">The resolved context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted text for the context.</returns>
    Task<string>  FormatContextForLlmAsync(AIResolvedContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats a single resource into text suitable for AI consumption.
    /// </summary>
    /// <param name="resource">The resolved resource.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Formatted text for the resource.</returns>
    Task<string> FormatResourceForLlmAsync(AIResolvedResource resource, CancellationToken cancellationToken = default);
}
