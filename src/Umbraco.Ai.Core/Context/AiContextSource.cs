namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Tracking information for where a context was resolved from.
/// Used for debugging and UI display.
/// </summary>
/// <param name="Level">The resolution level (e.g., "Profile", "Agent", "Prompt", "Content").</param>
/// <param name="EntityName">The name of the entity at that level (e.g., profile name, agent name).</param>
/// <param name="ContextName">The name of the context that was resolved.</param>
public sealed record AiContextSource(string Level, string? EntityName, string ContextName);
