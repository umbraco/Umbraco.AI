using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Agent.Core.EmbeddedAgents;
using Umbraco.Cms.Core.Models;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Service interface for agent management operations.
/// </summary>
public interface IAIAgentService
{
    /// <summary>
    /// Gets a agent by its unique identifier.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AIAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a agent by its alias.
    /// </summary>
    /// <param name="alias">The agent alias.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, null otherwise.</returns>
    Task<AIAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Agents.</returns>
    Task<IEnumerable<AIAgent>> GetAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of Agents with optional filtering.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="filter">Optional filter string for name/alias.</param>
    /// <param name="profileId">Optional profile ID filter.</param>
    /// <param name="surfaceId">Optional surface ID filter.</param>
    /// <param name="isActive">Optional active status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered/paginated agents and the total count.</returns>
    Task<(IEnumerable<AIAgent> Items, int Total)> GetAgentsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        AIAgentType? agentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents that belong to a specific surface.
    /// </summary>
    /// <param name="surfaceId">The surface ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agents that have the specified surface ID in their SurfaceIds.</returns>
    Task<IEnumerable<AIAgent>> GetAgentsBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a agent (insert if new, update if exists) with validation.
    /// If agent.Id is Guid.Empty, a new Guid will be generated.
    /// </summary>
    /// <param name="agent">The agent to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved agent.</returns>
    Task<AIAgent> SaveAgentAsync(AIAgent agent, CancellationToken cancellationToken = default);

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
    /// Checks whether any agents reference the specified profile.
    /// </summary>
    /// <param name="profileId">The profile ID to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if one or more agents reference the profile, otherwise false.</returns>
    Task<bool> AgentsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tools that are allowed for the specified agent.
    /// Includes system tools (always) + user tools matching agent configuration.
    /// If user group IDs are provided, applies user group permission overrides.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="userGroupIds">Optional user group IDs to resolve permission overrides. If null, uses current user's groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of allowed tool IDs.</returns>
    Task<IReadOnlyList<string>> GetAllowedToolIdsAsync(
        AIAgent agent,
        IEnumerable<Guid>? userGroupIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a specific tool call is permitted for the agent.
    /// If user group IDs are provided, applies user group permission overrides.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="toolId">The tool ID being called.</param>
    /// <param name="userGroupIds">Optional user group IDs to resolve permission overrides. If null, uses current user's groups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if tool is allowed, false otherwise.</returns>
    Task<bool> IsToolAllowedAsync(
        AIAgent agent,
        string toolId,
        IEnumerable<Guid>? userGroupIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects the most appropriate agent for a user prompt from agents available in the given context.
    /// </summary>
    /// <param name="userPrompt">The user's message to classify.</param>
    /// <param name="surfaceId">The surface ID to filter agents (e.g., "copilot").</param>
    /// <param name="context">The current availability context (section, entity type, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected agent, or null if no agents available in this context.</returns>
    Task<AIAgent?> SelectAgentForPromptAsync(
        string userPrompt,
        string surfaceId,
        AgentAvailabilityContext context,
        CancellationToken cancellationToken = default);

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
    /// <param name="frontendTools">Frontend tools with metadata for permission filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of AG-UI events.</returns>
    IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        Guid agentId,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams an agent execution with AG-UI events and execution options controlling overrides.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="request">The AG-UI run request containing messages, tools, and context.</param>
    /// <param name="frontendTools">Frontend tools with metadata for permission filtering.</param>
    /// <param name="options">Options controlling profile and context overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of AG-UI events.</returns>
    IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        Guid agentId,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        AIAgentExecutionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable embedded agent — an agent that runs purely in code without
    /// being managed through the backoffice UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned agent participates in the full middleware pipeline (auditing, tracking,
    /// guardrails, telemetry) and can use profiles and registered tools. It is not persisted
    /// and does not appear in the backoffice.
    /// </para>
    /// <para>
    /// The agent can be reused for multiple executions — each <c>RunAsync</c> or
    /// <c>RunStreamingAsync</c> call creates a fresh runtime context scope automatically.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Calling <c>RunAsync</c>/<c>RunStreamingAsync</c> directly on
    /// the returned agent does not publish <see cref="AIAgentExecutingNotification"/> or
    /// <see cref="AIAgentExecutedNotification"/>. Use <see cref="RunEmbeddedAgentAsync"/> or
    /// <see cref="StreamEmbeddedAgentAsync"/> for notification support.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the embedded agent via the builder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MAF <see cref="MsAIAgent"/> ready for use with RunAsync/RunStreamingAsync.</returns>
    Task<MsAIAgent> CreateEmbeddedAgentAsync(
        Action<AIEmbeddedAgentBuilder> configure,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an embedded agent with a one-shot execution, including notification publishing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience method that creates the embedded agent, publishes
    /// <see cref="AIAgentExecutingNotification"/>, executes the agent, and publishes
    /// <see cref="AIAgentExecutedNotification"/>.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the embedded agent via the builder.</param>
    /// <param name="messages">The chat messages to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent response.</returns>
    Task<AgentResponse> RunEmbeddedAgentAsync(
        Action<AIEmbeddedAgentBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams an embedded agent execution, including notification publishing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a convenience method that creates the embedded agent, publishes
    /// <see cref="AIAgentExecutingNotification"/>, streams the agent execution, and publishes
    /// <see cref="AIAgentExecutedNotification"/> when complete.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the embedded agent via the builder.</param>
    /// <param name="messages">The chat messages to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of agent response updates.</returns>
    IAsyncEnumerable<AgentResponseUpdate> StreamEmbeddedAgentAsync(
        Action<AIEmbeddedAgentBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}
