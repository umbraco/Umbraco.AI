using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Extracts embeddable text from published content.
/// </summary>
internal interface IContentTextExtractor
{
    /// <summary>
    /// Extracts text content suitable for embedding from the given published content.
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <returns>The extracted text, or null if no meaningful text could be extracted.</returns>
    string? ExtractText(IPublishedContent content);
}
