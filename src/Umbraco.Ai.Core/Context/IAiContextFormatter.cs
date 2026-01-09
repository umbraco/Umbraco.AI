namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Formats resolved context resources for injection into AI system prompts.
/// </summary>
public interface IAiContextFormatter
{
    /// <summary>
    /// Formats the entire resolved context into text suitable for AI consumption.
    /// </summary>
    /// <param name="context">The resolved context.</param>
    /// <returns>Formatted text for the context.</returns>
    string FormatContextForLlm(AiResolvedContext context);

    /// <summary>
    /// Formats a single resource into text suitable for AI consumption.
    /// </summary>
    /// <param name="resource">The resolved resource.</param>
    /// <returns>Formatted text for the resource.</returns>
    string FormatResourceForLlm(AiResolvedResource resource);
}
