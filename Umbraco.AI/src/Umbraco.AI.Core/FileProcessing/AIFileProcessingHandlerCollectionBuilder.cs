using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// An ordered collection builder for AI file processing handlers.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of file processing handlers:
/// <code>
/// builder.AIFileProcessingHandlers()
///     .Append&lt;OpenXmlFileProcessingHandler&gt;()
///     .InsertBefore&lt;OpenXmlFileProcessingHandler, CsvFileProcessingHandler&gt;();
/// </code>
/// The first handler where <see cref="IAIFileProcessingHandler.CanHandle"/> returns <c>true</c> wins.
/// </remarks>
public class AIFileProcessingHandlerCollectionBuilder
    : OrderedCollectionBuilderBase<AIFileProcessingHandlerCollectionBuilder, AIFileProcessingHandlerCollection, IAIFileProcessingHandler>
{
    /// <inheritdoc />
    protected override AIFileProcessingHandlerCollectionBuilder This => this;
}
