namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Tracking information for where a context was resolved from.
/// Used for debugging and UI display.
/// </summary>
/// <param name="ResolverName">The name of the resolver that provided the context.</param>
/// <param name="EntityName">The name of the entity at that level (e.g., profile name, agent name).</param>
/// <param name="ContextName">The name of the context that was resolved.</param>
public sealed record AIContextSource(string ResolverName, string? EntityName, string ContextName);
