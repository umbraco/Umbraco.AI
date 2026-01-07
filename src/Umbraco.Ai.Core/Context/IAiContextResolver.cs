namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Resolves AI context from multiple sources and merges them into a single resolved context.
/// </summary>
/// <remarks>
/// Resolution follows a hierarchical order: Profile → Agent → Prompt → Content.
/// Resources from later levels (Content) take precedence over earlier levels (Profile)
/// when duplicates exist.
/// </remarks>
public interface IAiContextResolver
{
    /// <summary>
    /// Resolves context from all applicable sources.
    /// </summary>
    /// <param name="request">The resolution request containing source identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The merged resolved context.</returns>
    Task<AiResolvedContext> ResolveAsync(
        AiContextResolutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves context for a specific profile.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved context for the profile.</returns>
    Task<AiResolvedContext> ResolveForProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);
}
