using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// A collection of chat middleware applied in order to AI chat clients.
/// </summary>
/// <remarks>
/// The order of middleware in this collection is controlled by the
/// <see cref="AiChatMiddlewareCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods.
/// </remarks>
public sealed class AiChatMiddlewareCollection : BuilderCollectionBase<IAiChatMiddleware>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiChatMiddlewareCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the middleware instances.</param>
    public AiChatMiddlewareCollection(Func<IEnumerable<IAiChatMiddleware>> items)
        : base(items)
    { }
}
