using Microsoft.Extensions.AI;
using Umbraco.Ai.Prompt.Core.Utils;

namespace Umbraco.Ai.Prompt.Core.Templates.Processors;

/// <summary>
/// Default template variable processor that resolves paths to text content.
/// Handles unprefixed variables like {{propertyAlias}} or {{entity.name}}.
/// </summary>
internal sealed class TextTemplateVariableProcessor : IAiTemplateVariableProcessor
{
    /// <inheritdoc />
    /// <remarks>
    /// Returns "*" to indicate this is the default processor for unprefixed variables.
    /// </remarks>
    public string Prefix => "*";

    /// <inheritdoc />
    public IEnumerable<AIContent> Process(string path, IReadOnlyDictionary<string, object?> context)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(context);

        var value = PathResolver.Resolve(context, path, new PathResolverOptions
        {
            CaseSensitive = false
        });

        if (value is null)
        {
            yield break;
        }

        var text = value.ToString() ?? string.Empty;
        yield return new TextContent(text);
    }
}
