using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Contexts.Resolvers;

/// <summary>
/// Defines a pluggable context resolver that contributes resources from a specific source.
/// </summary>
/// <remarks>
/// <para>
/// Resolvers are executed in order (controlled by <see cref="AiContextResolverCollectionBuilder"/>).
/// Later resolvers can override resources from earlier resolvers when duplicate IDs are encountered.
/// </para>
/// <para>
/// Each resolver should define its own key constants internally for reading from
/// <see cref="AiRuntimeContext"/>. For example, ProfileContextResolver
/// </para>
/// </remarks>
public interface IAiContextResolver
{
    /// <summary>
    /// Resolves context resources from this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolution result containing resources and source metadata, or an empty result if this resolver doesn't apply.</returns>
    Task<AiContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default);
}
