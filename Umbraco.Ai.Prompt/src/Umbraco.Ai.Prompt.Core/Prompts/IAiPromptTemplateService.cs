using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service for processing prompt templates with variable replacement.
/// Supports multimodal content through prefixed variables (e.g., {{image:propertyAlias}}).
/// </summary>
public interface IAiPromptTemplateService
{
    /// <summary>
    /// Processes a template and returns a list of content items.
    /// Supports both text variables ({{variable}}) and typed variables ({{prefix:path}}).
    /// </summary>
    /// <param name="template">The template containing variable placeholders.</param>
    /// <param name="context">Dictionary of values to replace in the template.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A list of <see cref="AIContent"/> items representing the processed template.
    /// Text segments are returned as <see cref="TextContent"/>.
    /// Image variables ({{image:path}}) are returned as <see cref="DataContent"/>.
    /// Adjacent text segments are consolidated into single <see cref="TextContent"/> items.
    /// </returns>
    Task<IEnumerable<AIContent>> ProcessTemplateAsync(string template, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default);
}
