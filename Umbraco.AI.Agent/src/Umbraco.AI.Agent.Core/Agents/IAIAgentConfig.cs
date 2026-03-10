namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Marker interface for agent type-specific configuration.
/// </summary>
/// <remarks>
/// Each <see cref="AIAgentType"/> has a corresponding config implementation:
/// <list type="bullet">
///   <item><see cref="AIStandardAgentConfig"/> for <see cref="AIAgentType.Standard"/></item>
///   <item><see cref="AIOrchestratedAgentConfig"/> for <see cref="AIAgentType.Orchestrated"/></item>
/// </list>
/// </remarks>
public interface IAIAgentConfig { }
