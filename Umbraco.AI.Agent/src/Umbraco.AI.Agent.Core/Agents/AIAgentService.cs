using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.InlineAgents;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Guardrails;
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
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

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
    private readonly IAGUIMessageConverter _messageConverter;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly AIToolCollection _toolCollection;
    private readonly IAIProfileService _profileService;
    private readonly IAIGuardrailService _guardrailService;
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
        IAGUIMessageConverter messageConverter,
        AIToolCollection toolCollection,
        IAIProfileService profileService,
        IAIGuardrailService guardrailService,
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
        _messageConverter = messageConverter;
        _toolCollection = toolCollection;
        _profileService = profileService;
        _guardrailService = guardrailService;
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
    public Task<(IEnumerable<AIAgent> Items, int Total)> GetAgentsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? surfaceId = null,
        bool? isActive = null,
        AIAgentType? agentType = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, surfaceId, isActive, agentType, cancellationToken);

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

        // Enforce type immutability
        if (existing is not null && existing.AgentType != agent.AgentType)
        {
            throw new InvalidOperationException($"Agent type cannot be changed after creation. Agent '{agent.Alias}' is a {existing.AgentType} agent.");
        }

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
    public Task<bool> AgentsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
        => _repository.ExistsWithProfileIdAsync(profileId, cancellationToken);

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

        // Get the classifier profile (falls back to default chat profile)
        AIProfile profile;
        try
        {
            profile = await _profileService.GetClassifierProfileAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // No classifier or default chat profile configured, fall back to first agent
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
    public Task<AgentResponse> RunAgentAsync(
        Guid agentId,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        => RunPersistedAgentAsync(agentId, messages, options ?? new AIAgentExecutionOptions(), cancellationToken);

    /// <inheritdoc />
    public async Task<AgentResponse> RunAgentAsync(
        string agentAlias,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentByAliasAsync(agentAlias, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with alias '{agentAlias}' not found.");

        return await RunPersistedAgentAsync(agent.Id, messages, options ?? new AIAgentExecutionOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(
        Guid agentId,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
        => StreamPersistedAgentAsync(agentId, messages, options ?? new AIAgentExecutionOptions(), cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(
        string agentAlias,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentByAliasAsync(agentAlias, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with alias '{agentAlias}' not found.");

        await foreach (var update in StreamPersistedAgentAsync(agent.Id, messages, options ?? new AIAgentExecutionOptions(), cancellationToken))
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAGUIEvent> StreamAgentAGUIAsync(
        Guid agentId,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        CancellationToken cancellationToken = default)
        => StreamAgentAGUIAsync(agentId, request, frontendTools, new AIAgentExecutionOptions(), cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<IAGUIEvent> StreamAgentAGUIAsync(
        Guid agentId,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        AIAgentExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // 1. Resolve agent
        var agent = await GetAgentAsync(agentId, cancellationToken);

        if (agent is null)
        {
            await foreach (var evt in EmitAGUIError(request, "Agent not found", "NOT_FOUND"))
            {
                yield return evt;
            }

            yield break;
        }

        if (!agent.IsActive)
        {
            await foreach (var evt in EmitAGUIError(request, $"Agent '{agent.Name}' is not active", "AGENT_NOT_ACTIVE"))
            {
                yield return evt;
            }

            yield break;
        }

        // Convert AG-UI messages to M.E.AI before publishing notification
        var chatMessages = _messageConverter.ConvertToChatMessages(request.Messages);

        // Prepare agent execution (profile override, notification, permissions, MAF agent creation)
        var context = await PrepareAgentExecutionAsync(
            agent, chatMessages, options, frontendTools,
            contextItems: _contextConverter.ConvertToRequestContextItems(request.Context),
            additionalProperties: new Dictionary<string, object?>
            {
                { Constants.ContextKeys.RunId, request.RunId },
                { Constants.ContextKeys.ThreadId, request.ThreadId },
                { CoreConstants.ContextKeys.LogKeys, new[] { Constants.ContextKeys.RunId, Constants.ContextKeys.ThreadId } }
            },
            cancellationToken);

        if (context is null)
        {
            // Notification was cancelled
            await foreach (var evt in EmitAGUIError(request, "Agent execution cancelled", "EXECUTION_CANCELLED"))
            {
                yield return evt;
            }

            yield break;
        }

        // Stream via AG-UI streaming service
        bool streamCompleted = false;
        try
        {
            await foreach (var evt in _streamingService.StreamAgentAsync(context.MafAgent, request, context.ConvertedFrontendTools, cancellationToken))
            {
                yield return evt;
            }
            streamCompleted = true;
        }
        finally
        {
            await PublishExecutedNotificationAsync(context, streamCompleted);
        }
    }

    /// <inheritdoc />
    public async Task<MsAIAgent> CreateInlineAgentAsync(
        Action<AIInlineAgentBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var (agent, builder) = await BuildAgentAsync(configure, cancellationToken);

        var additionalProperties = BuildAgentProperties(builder);

        return await _agentFactory.CreateAgentAsync(
            agent,
            builder.ContextItems,
            additionalTools: null,
            additionalProperties,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentResponse> RunAgentAsync(
        Action<AIInlineAgentBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var (agent, builder) = await BuildAgentAsync(configure, cancellationToken);
        var chatMessages = AsReadOnlyList(messages);

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIAgentExecutingNotification(agent, chatMessages, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline agent execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var additionalProperties = BuildAgentProperties(builder);
            var mafAgent = await _agentFactory.CreateAgentAsync(
                agent,
                builder.ContextItems,
                additionalTools: null,
                additionalProperties,
                cancellationToken);

            var response = await mafAgent.RunAsync(chatMessages, session: null, options: null, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIAgentExecutedNotification(
                agent,
                chatMessages,
                stopwatch.Elapsed,
                isSuccess,
                eventMessages)
                .WithStateFrom(executingNotification);

            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentResponseUpdate> StreamAgentAsync(
        Action<AIInlineAgentBuilder> configure,
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var (agent, builder) = await BuildAgentAsync(configure, cancellationToken);
        var chatMessages = AsReadOnlyList(messages);

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIAgentExecutingNotification(agent, chatMessages, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline agent execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var additionalProperties = BuildAgentProperties(builder);
            var mafAgent = await _agentFactory.CreateAgentAsync(
                agent,
                builder.ContextItems,
                additionalTools: null,
                additionalProperties,
                cancellationToken);

            await foreach (var update in mafAgent.RunStreamingAsync(chatMessages, session: null, options: null, cancellationToken))
            {
                yield return update;
            }

            isSuccess = true;
        }
        finally
        {
            var executedNotification = new AIAgentExecutedNotification(
                agent,
                chatMessages,
                stopwatch.Elapsed,
                isSuccess,
                eventMessages)
                .WithStateFrom(executingNotification);

            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    /// <summary>
    /// Builds a transient agent entity, resolving aliases and the "all tools" flag.
    /// </summary>
    private async Task<(AIAgent Agent, AIInlineAgentBuilder Builder)> BuildAgentAsync(
        Action<AIInlineAgentBuilder> configure,
        CancellationToken cancellationToken)
    {
        var builder = new AIInlineAgentBuilder();
        configure(builder);

        // If WithAllTools() was called, resolve all tool IDs from the collection
        if (builder.UseAllTools)
        {
            var allToolIds = _toolCollection.Select(t => t.Id).ToArray();
            builder.WithTools(allToolIds);
        }

        // Resolve profile alias to ID if needed
        if (builder.ProfileAlias is not null)
        {
            var profile = await _profileService.GetProfileByAliasAsync(builder.ProfileAlias, cancellationToken);
            if (profile is null)
            {
                throw new InvalidOperationException($"AI profile with alias '{builder.ProfileAlias}' not found.");
            }

            builder.SetResolvedProfileId(profile.Id);
        }

        // Resolve guardrail aliases to IDs if needed
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            var resolvedIds = new List<Guid>(aliases.Count);

            foreach (var alias in aliases)
            {
                var guardrail = await _guardrailService.GetGuardrailByAliasAsync(alias, cancellationToken);
                if (guardrail is null)
                {
                    throw new InvalidOperationException($"AI guardrail with alias '{alias}' not found.");
                }

                resolvedIds.Add(guardrail.Id);
            }

            builder.SetResolvedGuardrailIds(resolvedIds);
        }

        var agent = builder.Build();
        return (agent, builder);
    }

    /// <summary>
    /// Builds the additional properties dictionary for inline agent execution.
    /// Sets the feature type to "inline-agent" for audit/telemetry distinction.
    /// </summary>
    private static Dictionary<string, object?> BuildAgentProperties(AIInlineAgentBuilder builder)
    {
        var properties = new Dictionary<string, object?>
        {
            { CoreConstants.ContextKeys.FeatureType, CoreConstants.FeatureTypes.InlineAgent },
        };

        // Add ChatOptions override if set
        if (builder.ChatOptions is not null)
        {
            properties[CoreConstants.ContextKeys.ChatOptionsOverride] = builder.ChatOptions;
        }

        // Merge any additional properties from the builder
        if (builder.AdditionalProperties is not null)
        {
            foreach (var kvp in builder.AdditionalProperties)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        return properties;
    }

    /// <summary>
    /// Runs a persisted agent by ID with full orchestration.
    /// </summary>
    private async Task<AgentResponse> RunPersistedAgentAsync(
        Guid agentId,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions options,
        CancellationToken cancellationToken)
    {
        var agent = await ResolveActiveAgentAsync(agentId, cancellationToken);
        var chatMessages = AsReadOnlyList(messages);

        var context = await PrepareAgentExecutionAsync(
            agent, chatMessages, options, frontendTools: null,
            contextItems: options.ContextItems,
            additionalProperties: null,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("Agent execution cancelled by notification handler.");
        }

        bool isSuccess = false;
        try
        {
            var response = await context.MafAgent.RunAsync(chatMessages, session: null, options: null, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            await PublishExecutedNotificationAsync(context, isSuccess);
        }
    }

    /// <summary>
    /// Streams a persisted agent by ID with full orchestration.
    /// </summary>
    private async IAsyncEnumerable<AgentResponseUpdate> StreamPersistedAgentAsync(
        Guid agentId,
        IEnumerable<ChatMessage> messages,
        AIAgentExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agent = await ResolveActiveAgentAsync(agentId, cancellationToken);
        var chatMessages = AsReadOnlyList(messages);

        var context = await PrepareAgentExecutionAsync(
            agent, chatMessages, options, frontendTools: null,
            contextItems: options.ContextItems,
            additionalProperties: null,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("Agent execution cancelled by notification handler.");
        }

        bool isSuccess = false;
        try
        {
            await foreach (var update in context.MafAgent.RunStreamingAsync(chatMessages, session: null, options: null, cancellationToken))
            {
                yield return update;
            }
            isSuccess = true;
        }
        finally
        {
            await PublishExecutedNotificationAsync(context, isSuccess);
        }
    }

    /// <summary>
    /// Resolves a persisted agent by ID and validates it is active.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if agent not found or inactive.</exception>
    private async Task<AIAgent> ResolveActiveAgentAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await GetAgentAsync(agentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent with ID '{agentId}' not found.");

        if (!agent.IsActive)
        {
            throw new InvalidOperationException($"Agent '{agent.Name}' is not active.");
        }

        return agent;
    }

    /// <summary>
    /// Shared orchestration for persisted agent execution: applies overrides, publishes
    /// executing notification, resolves permissions, filters frontend tools, and creates the MAF agent.
    /// Returns null if the notification was cancelled.
    /// </summary>
    private async Task<AgentExecutionContext?> PrepareAgentExecutionAsync(
        AIAgent agent,
        IReadOnlyList<ChatMessage> chatMessages,
        AIAgentExecutionOptions options,
        IEnumerable<AIFrontendTool>? frontendTools,
        IEnumerable<AIRequestContextItem>? contextItems,
        Dictionary<string, object?>? additionalProperties,
        CancellationToken cancellationToken)
    {
        // Apply profile override if specified
        if (options.ProfileIdOverride.HasValue)
        {
            agent.ProfileId = options.ProfileIdOverride.Value;
        }

        // Publish executing notification (before execution)
        var eventMessages = new EventMessages();
        var executingNotification = new AIAgentExecutingNotification(agent, chatMessages, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            return null;
        }

        var stopwatch = Stopwatch.StartNew();

        // Resolve allowed tool IDs for permission checking
        var allowedToolIds = await GetAllowedToolIdsAsync(agent, options.UserGroupIds, cancellationToken);
        var allowedToolIdSet = new HashSet<string>(allowedToolIds, StringComparer.OrdinalIgnoreCase);
        var allowedScopeIds = agent.GetStandardConfig()?.AllowedToolScopeIds;

        // Convert and filter frontend tools by permissions
        IList<AITool>? convertedFrontendTools = null;
        if (frontendTools is not null)
        {
            var tools = new List<AITool>();

            foreach (var frontendTool in frontendTools)
            {
                var toolFunction = new Chat.AIFrontendToolFunction(
                    frontendTool.Tool,
                    frontendTool.Scope,
                    frontendTool.IsDestructive);

                bool isPermitted = allowedToolIdSet.Contains(frontendTool.Tool.Name)
                    || (frontendTool.Scope is not null
                        && (allowedScopeIds?.Contains(frontendTool.Scope, StringComparer.OrdinalIgnoreCase) ?? false));

                if (isPermitted)
                {
                    tools.Add(toolFunction);
                }
            }

            convertedFrontendTools = tools.Count > 0 ? tools : null;
        }

        // Build additional properties
        additionalProperties ??= new Dictionary<string, object?>();

        if (options.ContextIdsOverride is not null)
        {
            additionalProperties[Constants.ContextKeys.ContextIdsOverride] = options.ContextIdsOverride;
        }

        if (options.GuardrailIdsOverride is not null)
        {
            additionalProperties[AI.Core.Constants.ContextKeys.GuardrailIdsOverride] = options.GuardrailIdsOverride;
        }

        // Create MAF agent
        var mafAgent = await _agentFactory.CreateAgentAsync(
            agent,
            contextItems,
            convertedFrontendTools,
            additionalProperties,
            cancellationToken);

        return new AgentExecutionContext(
            agent,
            mafAgent,
            chatMessages,
            eventMessages,
            executingNotification,
            convertedFrontendTools,
            stopwatch);
    }

    /// <summary>
    /// Publishes the executed notification with duration and success status.
    /// </summary>
    private async Task PublishExecutedNotificationAsync(AgentExecutionContext context, bool isSuccess)
    {
        var executedNotification = new AIAgentExecutedNotification(
            context.Agent,
            context.ChatMessages,
            context.Stopwatch.Elapsed,
            isSuccess,
            context.EventMessages)
            .WithStateFrom(context.ExecutingNotification);

        await _eventAggregator.PublishAsync(executedNotification);
    }

    /// <summary>
    /// Emits a complete AG-UI error sequence: run started, error, run finished.
    /// </summary>
    private static async IAsyncEnumerable<IAGUIEvent> EmitAGUIError(
        AGUIRunRequest request,
        string message,
        string code)
    {
        var emitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
        yield return emitter.EmitRunStarted();
        yield return emitter.EmitError(message, code);
        yield return emitter.EmitRunFinished(new InvalidOperationException(message));
        await Task.CompletedTask; // Satisfy async enumerable contract
    }

    /// <summary>
    /// Returns the messages as an <see cref="IReadOnlyList{T}"/>, avoiding a copy when possible.
    /// </summary>
    private static IReadOnlyList<ChatMessage> AsReadOnlyList(IEnumerable<ChatMessage> messages)
        => messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

    private record AgentExecutionContext(
        AIAgent Agent,
        MsAIAgent MafAgent,
        IReadOnlyList<ChatMessage> ChatMessages,
        EventMessages EventMessages,
        AIAgentExecutingNotification ExecutingNotification,
        IList<AITool>? ConvertedFrontendTools,
        Stopwatch Stopwatch);
}
