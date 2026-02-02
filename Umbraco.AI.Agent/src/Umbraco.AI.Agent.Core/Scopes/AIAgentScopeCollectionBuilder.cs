using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Scopes;

/// <summary>
/// A collection builder for AI agent scopes.
/// </summary>
/// <remarks>
/// <para>
/// Scopes are auto-discovered via <see cref="AIAgentScopeAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add scopes manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered scopes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Composer
/// builder.AIAgentScopes()
///     .Add&lt;MyCopilotScope&gt;();
/// </code>
/// </example>
public class AIAgentScopeCollectionBuilder
    : LazyCollectionBuilderBase<AIAgentScopeCollectionBuilder, AIAgentScopeCollection, IAiAgentScope>
{
    /// <inheritdoc />
    protected override AIAgentScopeCollectionBuilder This => this;
}
