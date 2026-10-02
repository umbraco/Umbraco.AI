using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// A collection of decision middleware applied in order to AI decision clients.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AIDecisionMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionMiddlewareCollection : BuilderCollectionBase<IAIDecisionMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIDecisionMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AIDecisionMiddlewareCollection(Func<IEnumerable<IAIDecisionMiddleware>> items)
        : base(items)
    { }
}
