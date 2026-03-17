namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Defines a pluggable guardrail resolver that discovers which guardrails apply from a specific source.
/// </summary>
/// <remarks>
/// <para>
/// Resolvers are executed in order (controlled by <see cref="AIGuardrailResolverCollectionBuilder"/>).
/// Later resolvers can add additional guardrails.
/// </para>
/// <para>
/// Each resolver reads from <see cref="RuntimeContext.AIRuntimeContext"/> to determine which guardrails apply.
/// For example, <see cref="ProfileGuardrailResolver"/> reads the profile ID and loads its guardrail IDs.
/// </para>
/// </remarks>
public interface IAIGuardrailResolver
{
    /// <summary>
    /// Resolves guardrail rules from this source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolution result containing guardrail rules, or an empty result if this resolver doesn't apply.</returns>
    Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default);
}
