using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Agent.Core.Scopes;

/// <summary>
/// A collection builder for AI agent scopes.
/// </summary>
/// <remarks>
/// <para>
/// Scopes are auto-discovered via <see cref="AiAgentScopeAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add scopes manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered scopes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Composer
/// builder.AiAgentScopes()
///     .Add&lt;MyCopilotScope&gt;();
/// </code>
/// </example>
public class AiAgentScopeCollectionBuilder
    : LazyCollectionBuilderBase<AiAgentScopeCollectionBuilder, AiAgentScopeCollection, IAiAgentScope>
{
    /// <inheritdoc />
    protected override AiAgentScopeCollectionBuilder This => this;
}
