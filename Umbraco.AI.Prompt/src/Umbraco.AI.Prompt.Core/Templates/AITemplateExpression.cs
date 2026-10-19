using System.Text.RegularExpressions;

namespace Umbraco.AI.Prompt.Core.Templates;

/// <summary>
/// The syntax of <c>{{variable}}</c> expressions in prompt templates, shared by everything
/// that needs to read them so the rules stay in one place.
/// </summary>
internal static partial class AITemplateExpression
{
    /// <summary>
    /// The context key (and path) the TipTap toolbar uses for the user's selected text.
    /// </summary>
    internal const string SelectionPath = "selection";

    private const string ImagePrefix = "image";

    [GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
    internal static partial Regex VariablePattern();

    /// <summary>
    /// Splits the inner text of a <c>{{...}}</c> expression into an optional prefix and a path.
    /// </summary>
    internal static (string? Prefix, string Path) Parse(string expression)
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

    /// <summary>
    /// Whether the template reads the user's selected text, e.g. <c>{{selection}}</c> or <c>{{ Selection }}</c>.
    /// Follows the same rules the template service applies when resolving variables: whitespace inside the
    /// braces is ignored, paths are case-insensitive, and an unknown prefix falls back to the text processor.
    /// </summary>
    internal static bool ReferencesSelection(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return false;
        }

        foreach (Match match in VariablePattern().Matches(template))
        {
            var (prefix, path) = Parse(match.Groups[1].Value.Trim());

            // {{image:selection}} is routed to the image processor, which doesn't read the selection text.
            if (string.Equals(prefix, ImagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(path, SelectionPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
}
