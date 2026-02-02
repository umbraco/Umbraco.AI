using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Collection of runtime context contributors. Loops items and dispatches to handlers.
/// </summary>
public sealed class AIRuntimeContextContributorCollection : BuilderCollectionBase<IAIRuntimeContextContributor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIRuntimeContextContributorCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the contributor instances.</param>
    public AIRuntimeContextContributorCollection(Func<IEnumerable<IAIRuntimeContextContributor>> items)
        : base(items)
    { }

    /// <summary>
    /// Populates a runtime context by invoking all registered contributors in order.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    public void Populate(AIRuntimeContext context)
    {
        foreach (var contributor in this)
        {
            contributor.Contribute(context);
        }
    }
}
