using System.Text.Json;
using System.Text.RegularExpressions;
using Umbraco.Ai.Prompt.Core.Utils;

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
            var value = PathResolver.Resolve(context, variablePath, new PathResolverOptions
            {
                CaseSensitive = false
            });
            return value?.ToString() ?? string.Empty;
        });
    }
}
