using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// A collection builder for <see cref="IAIAgentUserGroupResolver"/>s.
/// </summary>
/// <remarks>
/// <code>
/// builder.AIAgentUserGroupResolvers().Append&lt;MyResolver&gt;();
/// </code>
/// </remarks>
public class AIAgentUserGroupResolverCollectionBuilder
    : OrderedCollectionBuilderBase<AIAgentUserGroupResolverCollectionBuilder, AIAgentUserGroupResolverCollection, IAIAgentUserGroupResolver>
{
    /// <inheritdoc />
    protected override AIAgentUserGroupResolverCollectionBuilder This => this;
}
