namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service for processing prompt templates with variable replacement.
/// </summary>
public interface IAiPromptTemplateService
{
    /// <summary>
    /// Replaces {{variable}} placeholders in a template with values from context.
    /// Supports dot notation for nested properties (e.g., {{entity.name}}).
    /// </summary>
    /// <param name="template">The template containing {{variable}} placeholders.</param>
    /// <param name="context">Dictionary of values to replace in the template.</param>
    /// <returns>The processed template with placeholders replaced.</returns>
    string ProcessTemplate(string template, IReadOnlyDictionary<string, object?> context);
}
