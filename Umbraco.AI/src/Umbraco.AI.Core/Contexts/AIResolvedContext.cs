namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Represents the fully resolved AI context after merging all applicable contexts
/// from profile, agent, prompt, and content levels.
/// </summary>
public sealed class AIResolvedContext
{
    /// <summary>
    /// Resources to inject directly into the system prompt.
    /// Includes resources with <see cref="AIContextResourceInjectionMode.Always"/>.
    /// </summary>
    public IReadOnlyList<AIResolvedResource> InjectedResources { get; init; } = [];

    /// <summary>
    /// Resources available via tool invocation.
    /// LLM can retrieve these on demand using context resource tools.
    /// Includes resources with <see cref="AIContextResourceInjectionMode.OnDemand"/>.
    /// </summary>
    public IReadOnlyList<AIResolvedResource> OnDemandResources { get; init; } = [];

    /// <summary>
    /// All resources from all resolved contexts, for reference and debugging.
    /// </summary>
    public IReadOnlyList<AIResolvedResource> AllResources { get; init; } = [];

    /// <summary>
    /// Tracking information showing where each context was resolved from.
    /// </summary>
    public IReadOnlyList<AIContextSource> Sources { get; init; } = [];

    /// <summary>
    /// Returns an empty resolved context.
    /// </summary>
    public static AIResolvedContext Empty => new();
}
