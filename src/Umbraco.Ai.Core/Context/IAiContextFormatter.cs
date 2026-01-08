namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Formats resolved context resources for injection into AI system prompts.
/// </summary>
public interface IAiContextFormatter
{
    /// <summary>
    /// Formats a resolved context into text suitable for system prompt injection.
    /// </summary>
    /// <param name="context">The resolved context.</param>
    /// <returns>Formatted text for the system prompt.</returns>
    string FormatForSystemPrompt(AiResolvedContext context);

    /// <summary>
    /// Formats a single resource into text suitable for AI consumption.
    /// </summary>
    /// <param name="resource">The resolved resource.</param>
    /// <returns>Formatted text for the resource.</returns>
    string FormatResource(AiResolvedResource resource);
}
