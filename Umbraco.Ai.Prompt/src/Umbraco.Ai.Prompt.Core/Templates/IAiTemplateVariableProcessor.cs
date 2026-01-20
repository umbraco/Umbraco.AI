using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Prompt.Core.Templates;

/// <summary>
/// Interface for processing template variables with specific prefixes.
/// Processors are responsible for resolving variables like {{prefix:path}} into <see cref="AIContent"/>.
/// </summary>
public interface IAiTemplateVariableProcessor
{
    /// <summary>
    /// Gets the prefix this processor handles (e.g., "image" for {{image:propertyAlias}}).
    /// Use "*" to indicate this is the default processor for unprefixed variables.
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// Processes a variable path and returns the appropriate content items.
    /// </summary>
    /// <param name="path">The variable path after the prefix (e.g., "umbracoFile" from {{image:umbracoFile}}).</param>
    /// <param name="context">The template context containing available values.</param>
    /// <returns>
    /// The content items to include in the message. May return multiple items (e.g., image + reference name).
    /// Return an empty enumerable if the variable cannot be resolved.
    /// </returns>
    IEnumerable<AIContent> Process(string path, IReadOnlyDictionary<string, object?> context);
}
