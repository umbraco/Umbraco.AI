using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Collection of registered <see cref="IAIFileThreadLifecycleProvider"/>s.
/// </summary>
public sealed class AIFileThreadLifecycleProviderCollection : BuilderCollectionBase<IAIFileThreadLifecycleProvider>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileThreadLifecycleProviderCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the provider instances.</param>
    public AIFileThreadLifecycleProviderCollection(Func<IEnumerable<IAIFileThreadLifecycleProvider>> items)
        : base(items)
    { }
}
