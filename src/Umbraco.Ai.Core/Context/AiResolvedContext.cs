namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Represents the fully resolved AI context after merging all applicable contexts
/// from profile, agent, prompt, and content levels.
/// </summary>
public sealed class AiResolvedContext
{
    /// <summary>
    /// Resources to inject directly into the system prompt.
    /// Includes resources with <see cref="AiContextResourceInjectionMode.Always"/>.
    /// </summary>
    public IReadOnlyList<AiResolvedResource> InjectedResources { get; init; } = [];

    /// <summary>
    /// Resources available via tool invocation.
    /// LLM can retrieve these on demand using context resource tools.
    /// Includes resources with <see cref="AiContextResourceInjectionMode.OnDemand"/>.
    /// </summary>
    public IReadOnlyList<AiResolvedResource> OnDemandResources { get; init; } = [];

    /// <summary>
    /// All resources from all resolved contexts, for reference and debugging.
    /// </summary>
    public IReadOnlyList<AiResolvedResource> AllResources { get; init; } = [];

    /// <summary>
    /// Tracking information showing where each context was resolved from.
    /// </summary>
    public IReadOnlyList<AiContextSource> Sources { get; init; } = [];

    /// <summary>
    /// Returns an empty resolved context.
    /// </summary>
    public static AiResolvedContext Empty => new();
}
