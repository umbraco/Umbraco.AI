namespace Umbraco.Ai.Core.Contexts;

/// <summary>
/// Defines how a context resource is injected into AI operations.
/// </summary>
public enum AiContextResourceInjectionMode
{
    /// <summary>
    /// Always included in system prompt.
    /// Use for essential brand guidelines, tone of voice, and core instructions that apply to every request.
    /// </summary>
    Always,

    /// <summary>
    /// Made available as a tool the LLM can invoke to retrieve content.
    /// The LLM decides when to look up the resource.
    /// </summary>
    OnDemand

    // V2: Semantic - Uses embedding similarity to determine relevance
}
