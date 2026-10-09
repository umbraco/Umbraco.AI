using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.ModelFacts;

/// <summary>
/// A collection of model fact providers.
/// </summary>
/// <remarks>
/// The order of providers in this collection is controlled by the
/// <see cref="AIModelFactProviderCollectionBuilder"/> using <c>Append</c>, <c>InsertBefore</c>,
/// and <c>InsertAfter</c> methods. Registration order decides the order facts are displayed in.
/// </remarks>
public sealed class AIModelFactProviderCollection : BuilderCollectionBase<IAIModelFactProvider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIModelFactProviderCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the provider instances.</param>
    public AIModelFactProviderCollection(Func<IEnumerable<IAIModelFactProvider>> items)
        : base(items)
    { }
}
