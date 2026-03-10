using Umbraco.AI.Agent.Core.Agents;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Convenience extension methods for <see cref="AIAgent"/>.
/// </summary>
public static class AIAgentExtensions
{
    /// <summary>
    /// Gets the standard agent config, or null if this is not a standard agent.
    /// </summary>
    public static AIStandardAgentConfig? GetStandardConfig(this AIAgent agent)
        => agent.Config as AIStandardAgentConfig;

    /// <summary>
    /// Gets the orchestrated agent config, or null if this is not an orchestrated agent.
    /// </summary>
    public static AIOrchestratedAgentConfig? GetOrchestratedConfig(this AIAgent agent)
        => agent.Config as AIOrchestratedAgentConfig;
}
