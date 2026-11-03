using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// A collection of extraction middleware applied in order to AI extraction clients.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AIExtractionMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionMiddlewareCollection : BuilderCollectionBase<IAIExtractionMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIExtractionMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AIExtractionMiddlewareCollection(Func<IEnumerable<IAIExtractionMiddleware>> items)
        : base(items)
    { }
}
