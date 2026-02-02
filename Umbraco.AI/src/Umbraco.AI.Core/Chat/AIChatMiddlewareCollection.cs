using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// A collection of chat middleware applied in order to AI chat clients.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AIChatMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
public sealed class AIChatMiddlewareCollection : BuilderCollectionBase<IAIChatMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIChatMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AIChatMiddlewareCollection(Func<IEnumerable<IAIChatMiddleware>> items)
        : base(items)
    { }
}
