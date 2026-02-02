namespace Umbraco.AI.Core.Contexts.Resolvers;

/// <summary>
/// Tracking information for where a context was resolved from within a resolver.
/// </summary>
/// <remarks>
/// Similar to <see cref="AIContextSource"/> but without <c>Level</c>, which is added
/// automatically by the aggregator using the resolver's type name.
/// </remarks>
/// <param name="EntityName">The name of the entity (e.g., profile name, agent name).</param>
/// <param name="ContextName">The name of the context that was resolved.</param>
public sealed record AIContextResolverSource(string? EntityName, string ContextName);
