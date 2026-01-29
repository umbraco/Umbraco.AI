using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Models;
using Umbraco.Cms.Core.Models;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Service interface for agent management operations.
/// </summary>
public interface IAiAgentService
{
    /// <summary>
    /// Gets a agent by its unique identifier.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AiAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a agent by its alias.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AiAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Agents.</returns>
    Task<IEnumerable<AiAgent>> GetAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of Agents with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="scopeId">Optional scope ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result containing Agents and total count.</returns>
    Task<PagedModel<AiAgent>> GetAgentsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? scopeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents that belong to a specific scope.
    /// </summary>
    /// <param name="scopeId">The scope ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agents that have the specified scope ID in their ScopeIds.</returns>
    Task<IEnumerable<AiAgent>> GetAgentsByScopeAsync(string scopeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a agent (insert if new, update if exists) with validation.
    /// If agent.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="agent">The agent to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved agent.</returns>
    Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a agent.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a agent with the given alias exists.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="excludeId">Optional ID to exclude from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if alias exists.</returns>
    Task<bool> AgentAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams an agent execution with AG-UI events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method orchestrates the complete agent lifecycle:
    /// <list type="bullet">
    ///   <item>Resolves the agent by ID or alias</item>
    ///   <item>Creates a runtime context scope</item>
    ///   <item>Populates context with contributors</item>
    ///   <item>Creates the MAF agent (inside the scope so context is available)</item>
    ///   <item>Streams AG-UI events from the agent execution</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="request">The AG-UI run request containing messages, tools, and context.</param>
    /// <param name="frontendToolDefinitions">Frontend tool definitions from the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of AG-UI events.</returns>
    IAsyncEnumerable<IAguiEvent> StreamAgentAsync(
        Guid agentId,
        AguiRunRequest request,
        IEnumerable<AguiTool>? frontendToolDefinitions,
        CancellationToken cancellationToken = default);
}
