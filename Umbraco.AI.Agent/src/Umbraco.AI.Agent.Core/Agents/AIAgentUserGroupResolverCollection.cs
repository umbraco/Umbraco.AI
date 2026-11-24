using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// A collection of <see cref="IAIAgentUserGroupResolver"/>s, one per <c>onBehalfOf</c> kind.
/// </summary>
public sealed class AIAgentUserGroupResolverCollection : BuilderCollectionBase<IAIAgentUserGroupResolver>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUserGroupResolverCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the resolver instances.</param>
    public AIAgentUserGroupResolverCollection(Func<IEnumerable<IAIAgentUserGroupResolver>> items)
        : base(items)
    { }
}
