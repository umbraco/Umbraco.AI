using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Collection of runtime context contributors. Loops items and dispatches to handlers.
/// </summary>
public sealed class AiRuntimeContextContributorCollection : BuilderCollectionBase<IAiRuntimeContextContributor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiRuntimeContextContributorCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the contributor instances.</param>
    public AiRuntimeContextContributorCollection(Func<IEnumerable<IAiRuntimeContextContributor>> items)
        : base(items)
    { }

    /// <summary>
    /// Populates a runtime context by invoking all registered contributors in order.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    public void Populate(AiRuntimeContext context)
    {
        foreach (var contributor in this)
        {
            contributor.Contribute(context);
        }
    }
}
