using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.Surfaces;

/// <summary>
/// A collection builder for AI agent surfaces.
/// </summary>
/// <remarks>
/// <para>
/// Surfaces are auto-discovered via <see cref="AIAgentSurfaceAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add surfaces manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered surfaces.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a Composer
/// builder.AIAgentSurfaces()
///     .Add&lt;MyCopilotSurface&gt;();
/// </code>
/// </example>
public class AIAgentSurfaceCollectionBuilder
    : LazyCollectionBuilderBase<AIAgentSurfaceCollectionBuilder, AIAgentSurfaceCollection, IAIAgentSurface>
{
    /// <inheritdoc />
    protected override AIAgentSurfaceCollectionBuilder This => this;
}
