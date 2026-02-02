using Microsoft.Extensions.AI;
using Umbraco.AI.Prompt.Core.Utils;

namespace Umbraco.AI.Prompt.Core.Templates.Processors;

/// <summary>
/// Default template variable processor that resolves paths to text content.
/// Handles unprefixed variables like {{propertyAlias}} or {{entity.name}}.
/// </summary>
internal sealed class TextTemplateVariableProcessor : IAITemplateVariableProcessor
{
    /// <inheritdoc />
    /// <remarks>
    /// Returns "*" to indicate this is the default processor for unprefixed variables.
    /// </remarks>
    public string Prefix => "*";

    /// <inheritdoc />
    public async Task<IEnumerable<AIContent>> ProcessAsync(string path, IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(context);
        
        var results = new List<AIContent>();

        var value = PathResolver.Resolve(context, path, new PathResolverOptions
        {
            CaseSensitive = false
        });

        if (value is null)
        {
            return results;
        }

        var text = value.ToString() ?? string.Empty;
        results.Add(new TextContent(text));
        return results;
    }
}
