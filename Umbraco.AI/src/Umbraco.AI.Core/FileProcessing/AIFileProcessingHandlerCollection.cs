using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// A collection of file processing handlers applied in order to process uploaded files.
/// </summary>
/// <remarks>
/// The order of handlers in this collection is controlled by the
/// <see cref="AIFileProcessingHandlerCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods. The first handler that can handle a given MIME type wins.
/// </remarks>
public sealed class AIFileProcessingHandlerCollection : BuilderCollectionBase<IAIFileProcessingHandler>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileProcessingHandlerCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the handler instances.</param>
    public AIFileProcessingHandlerCollection(Func<IEnumerable<IAIFileProcessingHandler>> items)
        : base(items)
    { }
}
