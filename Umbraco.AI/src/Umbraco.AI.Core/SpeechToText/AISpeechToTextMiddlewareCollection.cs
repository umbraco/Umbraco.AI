using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// A collection of speech-to-text middleware applied in order to AI speech-to-text clients.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AISpeechToTextMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
public sealed class AISpeechToTextMiddlewareCollection : BuilderCollectionBase<IAISpeechToTextMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISpeechToTextMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AISpeechToTextMiddlewareCollection(Func<IEnumerable<IAISpeechToTextMiddleware>> items)
        : base(items)
    { }
}
