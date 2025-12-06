using System.Text.RegularExpressions;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for processing prompt templates with variable replacement.
/// </summary>
internal sealed partial class AiPromptTemplateService : IAiPromptTemplateService
{
    [GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    /// <inheritdoc />
    public string ProcessTemplate(string template, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        return VariablePattern().Replace(template, match =>
        {
            var variablePath = match.Groups[1].Value.Trim();
            var value = GetNestedValue(context, variablePath);
            return value?.ToString() ?? string.Empty;
        });
    }

    /// <summary>
    /// Gets a nested value from a dictionary using dot notation.
    /// </summary>
    /// <param name="context">The context dictionary.</param>
    /// <param name="path">The dot-notation path (e.g., "entity.name").</param>
    /// <returns>The value at the path, or null if not found.</returns>
    private static object? GetNestedValue(IReadOnlyDictionary<string, object?> context, string path)
    {
        var parts = path.Split('.');
        object? current = context;

        foreach (var part in parts)
        {
            if (current is null)
            {
                return null;
            }

            if (current is IReadOnlyDictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else if (current is IDictionary<string, object?> mutableDict)
            {
                if (!mutableDict.TryGetValue(part, out current))
                {
                    return null;
                }
            }
            else
            {
                // Try to get property value via reflection for object types
                var property = current.GetType().GetProperty(part);
                if (property is null)
                {
                    return null;
                }
                current = property.GetValue(current);
            }
        }

        return current;
    }
}
