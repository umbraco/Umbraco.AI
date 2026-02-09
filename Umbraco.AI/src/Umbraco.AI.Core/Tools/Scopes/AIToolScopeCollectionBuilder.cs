using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools.Scopes;

/// <summary>
/// A collection builder for AI tool scopes.
/// </summary>
/// <remarks>
/// <para>
/// Scopes are auto-discovered via <see cref="AIToolScopeAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add scopes manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered scopes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Composer
/// builder.AIToolScopes()
///     .Add&lt;MyCustomScope&gt;();
/// </code>
/// </example>
public class AIToolScopeCollectionBuilder
    : LazyCollectionBuilderBase<AIToolScopeCollectionBuilder, AIToolScopeCollection, IAIToolScope>
{
    /// <inheritdoc />
    protected override AIToolScopeCollectionBuilder This => this;
}
