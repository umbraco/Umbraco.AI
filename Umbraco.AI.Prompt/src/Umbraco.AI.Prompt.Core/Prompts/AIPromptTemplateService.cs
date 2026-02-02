using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Prompt.Core.Templates;
using Umbraco.AI.Prompt.Core.Templates.Processors;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Service implementation for processing prompt templates with variable replacement.
/// Supports multimodal content through prefixed variables (e.g., {{image:propertyAlias}}).
/// </summary>
internal sealed partial class AIPromptTemplateService : IAiPromptTemplateService
{
    [GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    private readonly Dictionary<string, IAiTemplateVariableProcessor> _processors;

    public AIPromptTemplateService(
        TextTemplateVariableProcessor textProcessor,
        ImageTemplateVariableProcessor imageProcessor)
    {
        _processors = new Dictionary<string, IAiTemplateVariableProcessor>(StringComparer.OrdinalIgnoreCase)
        {
            [imageProcessor.Prefix] = imageProcessor,
            [textProcessor.Prefix] = textProcessor
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIContent>> ProcessTemplateAsync(string template, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        var results = new List<AIContent>();
        var regex = VariablePattern();
        var matches = regex.Matches(template);

        var lastIndex = 0;
        var pendingText = new StringBuilder();

        foreach (Match match in matches)
        {
            // Add any literal text before this match
            if (match.Index > lastIndex)
            {
                pendingText.Append(template.AsSpan(lastIndex, match.Index - lastIndex));
            }

            // Parse the variable expression
            var expression = match.Groups[1].Value.Trim();
            var (prefix, path) = ParseExpression(expression);

            // Route to appropriate processor and get content items
            var contentItems = await ProcessVariableAsync(prefix, path, context, cancellationToken);

            foreach (var content in contentItems)
            {
                if (content is TextContent textContent)
                {
                    // Accumulate text content
                    pendingText.Append(textContent.Text);
                }
                else
                {
                    // Non-text content (e.g., DataContent for images)
                    // Flush any pending text first
                    if (pendingText.Length > 0)
                    {
                        results.Add(new TextContent(pendingText.ToString()));
                        pendingText.Clear();
                    }

                    results.Add(content);
                }
            }

            lastIndex = match.Index + match.Length;
        }

        // Add any remaining literal text after the last match
        if (lastIndex < template.Length)
        {
            pendingText.Append(template.AsSpan(lastIndex));
        }

        // Flush any remaining pending text
        if (pendingText.Length > 0)
        {
            results.Add(new TextContent(pendingText.ToString()));
        }

        return results;
    }

    private static (string? Prefix, string Path) ParseExpression(string expression)
    {
        // Check for prefix:path format
        var colonIndex = expression.IndexOf(':');
        if (colonIndex > 0)
        {
            var prefix = expression[..colonIndex].Trim();
            var path = expression[(colonIndex + 1)..].Trim();

            // Only treat as prefix if it looks like an identifier (no dots, brackets, etc.)
            if (IsValidPrefix(prefix))
            {
                return (prefix, path);
            }
        }

        // No prefix, use entire expression as path
        return (null, expression);
    }

    private static bool IsValidPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return false;
        }

        // A valid prefix is a simple identifier (letters, digits, underscores)
        foreach (var c in prefix)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                return false;
            }
        }

        return true;
    }

    private Task<IEnumerable<AIContent>> ProcessVariableAsync(string? prefix, string path, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken)
    {
        var targetPrefix = prefix ?? "*";

        // Try to find a processor for the prefix, fall back to default "*" processor for unknown prefixes
        if (!_processors.TryGetValue(targetPrefix, out var processor))
        {
            processor = _processors["*"];
        }

        return processor.ProcessAsync(path, context, cancellationToken);
    }
}
