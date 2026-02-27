using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Service implementation for agent management operations.
/// </summary>
internal sealed class AIAgentService : IAIAgentService
{
    private readonly IAIAgentRepository _repository;
    private readonly IAIEntityVersionService _versionService;
    private readonly IAIAgentFactory _agentFactory;
    private readonly IAGUIStreamingService _streamingService;
    private readonly IAGUIContextConverter _contextConverter;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly AIToolCollection _toolCollection;
    private readonly IAIProfileService _profileService;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly AIAgentScopeValidator _scopeValidator;
    private readonly AIAgentSurfaceCollection _surfaceCollection;
    private readonly IEventAggregator _eventAggregator;

    public AIAgentService(
        IAIAgentRepository repository,
        IAIEntityVersionService versionService,
        IAIAgentFactory agentFactory,
        IAGUIStreamingService streamingService,
        IAGUIContextConverter contextConverter,
        AIToolCollection toolCollection,
        IAIProfileService profileService,
        IAIChatClientFactory chatClientFactory,
        AIAgentScopeValidator scopeValidator,
        AIAgentSurfaceCollection surfaceCollection,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _agentFactory = agentFactory;
        _streamingService = streamingService;
        _contextConverter = contextConverter;
        _toolCollection = toolCollection;
        _profileService = profileService;
        _chatClientFactory = chatClientFactory;
        _scopeValidator = scopeValidator;
        _surfaceCollection = surfaceCollection;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AIAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AIAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AIAgent>> GetAgentsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, surfaceId, isActive, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetAgentsBySurfaceAsync(string surfaceId, CancellationToken cancellationToken = default)
        => _repository.GetBySurfaceAsync(surfaceId, cancellationToken);

    /// <inheritdoc />
    public async Task<AIAgent> SaveAgentAsync(AIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(agent.Alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(agent.Name);

        // Generate new ID if needed
        if (agent.Id == Guid.Empty)
        {
            agent.Id = Guid.NewGuid();
        }

        // Check for alias uniqueness
        var existingByAlias = await _repository.GetByAliasAsync(agent.Alias, cancellationToken);
        if (existingByAlias is not null && existingByAlias.Id != agent.Id)
        {
            throw new InvalidOperationException($"A agent with alias '{agent.Alias}' already exists.");
        }

        var userId = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

        // Publish saving notification (before save)
        var messages = new EventMessages();
        var savingNotification = new AIAgentSavingNotification(agent, messages);
        await _eventAggregator.PublishAsync(savingNotification, cancellationToken);

        // Check if cancelled
        if (savingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Agent save cancelled: {errorMessages}");
        }

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(agent.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        // Perform save
        var savedAgent = await _repository.SaveAsync(agent, userId, cancellationToken);

        // Publish saved notification (after save)
        var savedNotification = new AIAgentSavedNotification(savedAgent, messages)
            .WithStateFrom(savingNotification);
        await _eventAggregator.PublishAsync(savedNotification, cancellationToken);

        return savedAgent;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Publish deleting notification (before delete)
        var messages = new EventMessages();
        var deletingNotification = new AIAgentDeletingNotification(id, messages);
        await _eventAggregator.PublishAsync(deletingNotification, cancellationToken);

        // Check if cancelled
        if (deletingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", messages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Agent delete cancelled: {errorMessages}");
        }

        // Perform delete
        var result = await _repository.DeleteAsync(id, cancellationToken);

        // Publish deleted notification (after delete)
        var deletedNotification = new AIAgentDeletedNotification(id, messages)
            .WithStateFrom(deletingNotification);
        await _eventAggregator.PublishAsync(deletedNotification, cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public Task<bool> AgentAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => _repository.AliasExistsAsync(alias, excludeId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllowedToolIdsAsync(
        AIAgent agent,
        IEnumerable<Guid>? userGroupIds = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve user groups if not provided
        var resolvedUserGroupIds = userGroupIds ?? await GetCurrentUserGroupIdsAsync(cancellationToken);

        var result = AIAgentToolHelper.GetAllowedToolIds(agent, _toolCollection, resolvedUserGroupIds);
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsToolAllowedAsync(
        AIAgent agent,
        string toolId,
        IEnumerable<Guid>? userGroupIds = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve user groups if not provided
        var resolvedUserGroupIds = userGroupIds ?? await GetCurrentUserGroupIdsAsync(cancellationToken);

        var result = AIAgentToolHelper.IsToolAllowed(agent, toolId, _toolCollection, resolvedUserGroupIds);
        return result;
    }

    /// <inheritdoc />
    public async Task<AIAgent?> SelectAgentForPromptAsync(
        string userPrompt,
        string surfaceId,
        AgentAvailabilityContext context,
        CancellationToken cancellationToken = default)
    {
        // 1. Get all agents in the surface
        var allAgents = await GetAgentsBySurfaceAsync(surfaceId, cancellationToken);

        // 2. Get the surface for scope validation
        var surface = _surfaceCollection.FirstOrDefault(s => string.Equals(s.Id, surfaceId, StringComparison.OrdinalIgnoreCase));

        // 3. Filter to only active agents that are available in the current context
        var availableAgents = allAgents
            .Where(a => a.IsActive && _scopeValidator.IsAgentAvailable(a, context, surface))
            .ToList();

        // 4. If no agents available, return null
        if (availableAgents.Count == 0)
        {
            return null;
        }

        // 5. If only one agent, return it directly (no LLM call needed)
        if (availableAgents.Count == 1)
        {
            return availableAgents[0];
        }

        // 6. Multiple agents - use LLM to classify
        var classificationPrompt = BuildClassificationPrompt(availableAgents, userPrompt);

        // Get the default chat profile
        var profile = await _profileService.GetDefaultProfileAsync(AICapability.Chat, cancellationToken);
        if (profile is null)
        {
            // No default profile available, fall back to first agent
            return availableAgents[0];
        }

        // Create chat client
        var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);

        // Send classification prompt
        var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, classificationPrompt)], options: null, cancellationToken);
        var responseText = response.Text ?? string.Empty;

        // Parse the GUID from the response
        var selectedAgentId = ParseAgentIdFromResponse(responseText);

        if (selectedAgentId.HasValue)
        {
            var selectedAgent = availableAgents.FirstOrDefault(a => a.Id == selectedAgentId.Value);
            if (selectedAgent is not null)
            {
                return selectedAgent;
            }
        }

        // Fallback to first agent if parsing fails
        return availableAgents[0];
    }

    /// <summary>
    /// Builds a classification prompt for agent selection.
    /// </summary>
    private static string BuildClassificationPrompt(IList<AIAgent> agents, string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an agent router. Given the user's message, select the most appropriate agent.");
        sb.AppendLine("Return ONLY the agent ID (the GUID) on a single line, nothing else.");
        sb.AppendLine();
        sb.AppendLine("Available agents:");

        foreach (var agent in agents)
        {
            var description = string.IsNullOrWhiteSpace(agent.Description)
                ? "No description"
                : agent.Description;

            sb.AppendLine($"[{agent.Id}] {agent.Name}: {description}");
        }

        sb.AppendLine();
        sb.AppendLine($"User message: {userPrompt}");

        return sb.ToString();
    }

    /// <summary>
    /// Parses an agent ID (GUID) from the LLM response.
    /// </summary>
    private static Guid? ParseAgentIdFromResponse(string response)
    {
        // Try to find a GUID in the response using regex
        var guidPattern = @"[{(]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[)}]?";
        var match = Regex.Match(response, guidPattern);

        if (match.Success && Guid.TryParse(match.Value, out var agentId))
        {
            return agentId;
        }

        return null;
    }

    /// <summary>
    /// Gets the current user's user group IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user group IDs for the current user. Empty list if no user or no groups.</returns>
    private Task<IReadOnlyList<Guid>> GetCurrentUserGroupIdsAsync(CancellationToken cancellationToken)
    {
        var user = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser;
        if (user is null)
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        var groupIds = user.Groups.Select(g => g.Key).ToList();
        return Task.FromResult<IReadOnlyList<Guid>>(groupIds);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IAGUIEvent> StreamAgentAsync(
        Guid agentId,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. Resolve agent
        var agent = await GetAgentAsync(agentId, cancellationToken);

        if (agent is null)
        {
            // Emit error event and finish
            var errorEmitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
            yield return errorEmitter.EmitRunStarted();
            yield return errorEmitter.EmitError("Agent not found", "NOT_FOUND");
            yield return errorEmitter.EmitRunFinished(new InvalidOperationException("Agent not found"));
            yield break;
        }

        if (!agent.IsActive)
        {
            var errorEmitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
            yield return errorEmitter.EmitRunStarted();
            yield return errorEmitter.EmitError($"Agent '{agent.Name}' is not active", "AGENT_NOT_ACTIVE");
            yield return errorEmitter.EmitRunFinished(new InvalidOperationException($"Agent '{agent.Name}' is not active"));
            yield break;
        }

        // Publish executing notification (before execution)
        var eventMessages = new EventMessages();
        var executingNotification = new AIAgentExecutingNotification(agent, request, frontendTools, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        // Check if cancelled
        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            var errorEmitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
            yield return errorEmitter.EmitRunStarted();
            yield return errorEmitter.EmitError($"Agent execution cancelled: {errorMessages}", "EXECUTION_CANCELLED");
            yield return errorEmitter.EmitRunFinished(new InvalidOperationException($"Agent execution cancelled: {errorMessages}"));
            yield break;
        }

        // Track execution duration with high-resolution timer
        var stopwatch = Stopwatch.StartNew();

        // 2. Convert AG-UI context and create frontend tools with permission filtering
        var contextItems = _contextConverter.ConvertToRequestContextItems(request.Context);

        // Get allowed tool IDs for permission checking (uses current user's groups)
        var allowedToolIds = await GetAllowedToolIdsAsync(agent, userGroupIds: null, cancellationToken);

        // Convert AIFrontendTools to AIFrontendToolFunction and filter by permissions
        IList<AITool>? convertedFrontendTools = null;
        if (frontendTools is not null)
        {
            var tools = new List<AITool>();

            foreach (var frontendTool in frontendTools)
            {
                // Create AIFrontendToolFunction with metadata already attached
                var toolFunction = new Chat.AIFrontendToolFunction(
                    frontendTool.Tool,
                    frontendTool.Scope,
                    frontendTool.IsDestructive);

                // Check if tool is permitted
                bool isPermitted = false;

                // Check if tool ID is explicitly allowed
                if (allowedToolIds.Contains(frontendTool.Tool.Name, StringComparer.OrdinalIgnoreCase))
                {
                    isPermitted = true;
                }
                // Check if tool has scope and scope is allowed
                else if (frontendTool.Scope is not null && agent.AllowedToolScopeIds.Contains(frontendTool.Scope, StringComparer.OrdinalIgnoreCase))
                {
                    isPermitted = true;
                }

                if (isPermitted)
                {
                    tools.Add(toolFunction);
                }
            }

            convertedFrontendTools = tools.Count > 0 ? tools : null;
        }

        // 3. Build additional properties for telemetry/logging
        var additionalProperties = new Dictionary<string, object?>
        {
            { Constants.ContextKeys.RunId, request.RunId },
            { Constants.ContextKeys.ThreadId, request.ThreadId },
            { CoreConstants.ContextKeys.LogKeys, new[]
            {
                Constants.ContextKeys.RunId,
                Constants.ContextKeys.ThreadId
            }}
        };

        // 4. Create MAF agent
        //    System message injection is handled automatically by ScopedAIAgent
        var agentInst = await _agentFactory.CreateAgentAsync(
            agent,
            contextItems,
            convertedFrontendTools,
            additionalProperties,
            cancellationToken);

        // 5. Stream via AG-UI streaming service and publish executed notification when done
        //    No additionalSystemPrompt needed - ScopedAIAgent handles it
        bool streamCompleted = false;
        try
        {
            await foreach (var evt in _streamingService.StreamAgentAsync(agentInst, request, convertedFrontendTools, cancellationToken))
            {
                yield return evt;
            }
            streamCompleted = true;
        }
        finally
        {
            // Calculate duration using high-resolution timer
            var duration = stopwatch.Elapsed;

            // Publish executed notification (after execution completes or fails)
            var executedNotification = new AIAgentExecutedNotification(
                agent,
                request,
                frontendTools,
                duration,
                streamCompleted, // isSuccess based on whether stream completed
                eventMessages)
                .WithStateFrom(executingNotification);

            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }
}
