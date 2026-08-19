using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Agent.Core.InlineAgents;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Extensions;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

using AgentConstants = Umbraco.AI.Agent.Core.Constants;
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
    private readonly IAIContextService _contextService;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly AIAgentScopeValidator _scopeValidator;
    private readonly AIAgentSurfaceCollection _surfaceCollection;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILoggerFactory? _loggerFactory;

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
        IAIContextService contextService,
        IAIChatClientFactory chatClientFactory,
        AIAgentScopeValidator scopeValidator,
        AIAgentSurfaceCollection surfaceCollection,
        IEventAggregator eventAggregator,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null,
        ILoggerFactory? loggerFactory = null)
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
        _contextService = contextService;
        _chatClientFactory = chatClientFactory;
        _scopeValidator = scopeValidator;
        _surfaceCollection = surfaceCollection;
        _eventAggregator = eventAggregator;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _loggerFactory = loggerFactory;
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

        // Prepare agent execution (profile override, notification, permissions, MAF agent creation).
        // AG-UI is the interactive surface — it can emit a human_approval interrupt and resume,
        // so destructive tools are gated for real approval regardless of the options default.
        // Start from the AG-UI-specific keys, then forward any caller-supplied
        // options.AdditionalProperties (e.g. a Copilot Workspace project's context/resources) so they
        // reach the runtime context — matching the persisted Run/Stream paths, which was previously
        // dropped on this path.
        var additionalProperties = new Dictionary<string, object?>
        {
            { Constants.ContextKeys.RunId, request.RunId },
            { Constants.ContextKeys.ThreadId, request.ThreadId },
            { CoreConstants.ContextKeys.LogKeys, new[] { Constants.ContextKeys.RunId, Constants.ContextKeys.ThreadId } }
        };

        if (options.AdditionalProperties is not null)
        {
            foreach (var property in options.AdditionalProperties)
            {
                additionalProperties[property.Key] = property.Value;
            }
        }

        var context = await PrepareAgentExecutionAsync(
            agent, chatMessages, options, frontendTools,
            contextItems: _contextConverter.ConvertToRequestContextItems(request.Context),
            additionalProperties: additionalProperties,
            approvalPolicy: AIApprovalPolicy.Interactive,
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

        // When bound to a persisted conversation, create the run's session and bind it (the concrete
        // binding lives with the consumer's provider) so the attached ChatHistoryProvider loads/stores
        // against the right conversation. Non-persisted surfaces stream with a null session as before.
        AgentSession? session = null;
        IReadOnlyDictionary<string, ToolApprovalRequestContent>? pendingApprovalCalls = null;
        if (options.ConversationHistory is { } historyBinding)
        {
            // A fresh session is created per HTTP request (Copilot Workspace persists conversation
            // history in its own store rather than keeping a MAF session alive between requests — it
            // wouldn't survive a restart anyway). But session-scoped decorators (e.g. the tool-approval-
            // response binder introduced in Microsoft.Agents.AI 1.14) record their own state directly on
            // the session object, not in chat history — so restore the prior run's state here rather than
            // always starting bare, or that state silently never reaches the decorator. Confirmed
            // empirically necessary: disabling that decorator instead (relying solely on our own
            // persisted-history-based approval correlation) breaks a chained multi-approval turn — e.g.
            // create_umbraco_content then publish_umbraco_content approved back-to-back in one
            // conversation — because a prior turn's already-resolved approval request resurfaces as
            // unmatched once a later turn's persisted history is reloaded. Keeping this decorator active
            // (with its state correctly restored) avoids that; our own correlation layer stays in place
            // too, since it's what supplies the correct request object for CreateResponse().
            var persistedState = historyBinding.LoadSessionState is { } loadState
                ? await loadState(cancellationToken)
                : null;
            session = persistedState is { } state
                ? await context.MafAgent.DeserializeSessionAsync(state, cancellationToken: cancellationToken)
                : await context.MafAgent.CreateSessionAsync(cancellationToken);
            historyBinding.BindSession(session);

            // For an approval resume after a reload, the original tool call may only exist in persisted
            // history — recover it (name + args) so the resume path can correlate instead of skipping (B2).
            pendingApprovalCalls = await ResolvePendingApprovalCallsAsync(historyBinding, request, cancellationToken);
        }

        // Stream via AG-UI streaming service
        bool streamCompleted = false;
        try
        {
            await foreach (var evt in _streamingService.StreamAgentAsync(context.MafAgent, request, context.ConvertedFrontendTools, session, pendingApprovalCalls, cancellationToken))
            {
                yield return evt;
            }
            streamCompleted = true;
        }
        finally
        {
            // Persist session state (success or interrupt) so the next request's fresh session can
            // restore it above — see the restore comment for why this matters.
            if (options.ConversationHistory is { SaveSessionState: { } saveState } binding && session is not null)
            {
                var serialized = await context.MafAgent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
                await saveState(serialized, cancellationToken);
            }

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

        // Inline agents are created for programmatic callers with no interactive surface to
        // resolve a human_approval interrupt — deny destructive tools so runs don't stall.
        return await _agentFactory.CreateAgentAsync(
            agent,
            builder.ContextItems,
            additionalTools: null,
            additionalProperties,
            approvalPolicy: AIApprovalPolicy.DenyAll,
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
        string? responseText = null;
        Exception? capturedException = null;

        try
        {
            var additionalProperties = BuildAgentProperties(builder);
            // Non-interactive programmatic execution — deny destructive tools (no resume surface).
            var mafAgent = await _agentFactory.CreateAgentAsync(
                agent,
                builder.ContextItems,
                additionalTools: null,
                additionalProperties,
                approvalPolicy: AIApprovalPolicy.DenyAll,
                cancellationToken);

            var response = await mafAgent.RunAsync(chatMessages, session: null, options: null, cancellationToken);
            responseText = response.Text;
            isSuccess = true;
            return response;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            var executedNotification = new AIAgentExecutedNotification(
                agent,
                chatMessages,
                stopwatch.Elapsed,
                isSuccess,
                eventMessages)
                {
                    ResponseText = responseText,
                    Exception = capturedException,
                }
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
            // Programmatic streaming (not AG-UI) has no resume surface — deny destructive tools.
            var mafAgent = await _agentFactory.CreateAgentAsync(
                agent,
                builder.ContextItems,
                additionalTools: null,
                additionalProperties,
                approvalPolicy: AIApprovalPolicy.DenyAll,
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
            builder.SetResolvedProfileId(
                await _profileService.GetProfileIdByAliasAsync(builder.ProfileAlias, cancellationToken));
        }

        // Resolve guardrail aliases to IDs if needed
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            builder.SetResolvedGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(aliases, cancellationToken));
        }

        // Resolve additional guardrail aliases to IDs if needed
        if (builder.AdditionalGuardrailAliases is { Count: > 0 } additionalGuardrailAliases)
        {
            builder.SetResolvedAdditionalGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(additionalGuardrailAliases, cancellationToken));
        }

        // Resolve context aliases to IDs if needed (replace)
        if (builder.ContextAliases is { Count: > 0 } contextAliases)
        {
            builder.SetResolvedContextIds(
                await _contextService.GetContextIdsByAliasesAsync(contextAliases, cancellationToken));
        }

        // Resolve additional context aliases to IDs if needed (additive)
        if (builder.AdditionalContextAliases is { Count: > 0 } additionalContextAliases)
        {
            builder.SetResolvedAdditionalContextIds(
                await _contextService.GetContextIdsByAliasesAsync(additionalContextAliases, cancellationToken));
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

        // SetGuardrails → replace: the override key suppresses both agent and profile guardrail
        // resolvers; only the override list applies. Additive (WithGuardrails) lives on agent.GuardrailIds.
        if (builder.GuardrailIds.Count > 0)
        {
            properties[CoreConstants.ContextKeys.GuardrailIdsOverride] = builder.GuardrailIds;
        }

        // SetContexts → replace: the override key suppresses both agent and profile context resolvers;
        // only the override list applies. Additive (WithContexts) lives on AIStandardAgentConfig.ContextIds.
        if (builder.ContextIds is not null)
        {
            properties[CoreConstants.ContextKeys.ContextIdsOverride] = builder.ContextIds;
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
    /// Builds the additional-properties dictionary forwarded to <see cref="PrepareAgentExecutionAsync"/>
    /// from execution options: <see cref="AIAgentExecutionOptions.OutputSchema"/> becomes a
    /// <see cref="CoreConstants.ContextKeys.ChatOptionsOverride"/> entry, and any caller-supplied
    /// <see cref="AIAgentExecutionOptions.AdditionalProperties"/> entries are copied through.
    /// Returns null when both inputs are null so the existing "no extra props" path is preserved.
    /// </summary>
    private static Dictionary<string, object?>? BuildAdditionalPropertiesFromOptions(AIAgentExecutionOptions options)
    {
        if (options.OutputSchema is null && options.AdditionalProperties is null)
        {
            return null;
        }

        var properties = new Dictionary<string, object?>();

        if (options.OutputSchema is not null)
        {
            properties[CoreConstants.ContextKeys.ChatOptionsOverride] = new ChatOptions
            {
                ResponseFormat = options.OutputSchema.ResponseFormat,
            };
        }

        if (options.AdditionalProperties is not null)
        {
            foreach (var kvp in options.AdditionalProperties)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        return properties;
    }

    /// <summary>
    /// Returns a <c>LogKeys</c> string array containing the existing keys (if any) plus
    /// <paramref name="keyToAppend"/>, de-duplicated. Used to ensure a newly-added context key is
    /// persisted to the audit log without dropping keys the caller already requested.
    /// </summary>
    private static string[] AppendLogKey(object? existingLogKeys, string keyToAppend)
    {
        var keys = existingLogKeys as IEnumerable<string> ?? [];
        return keys.Append(keyToAppend).Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// For an approval-resume request, resolves the original tool calls for the resume entries' approval
    /// callIds from persisted history (via the binding), so the streaming resume path can correlate a
    /// reloaded approval instead of skipping it. Returns null when there is nothing to resolve (B2).
    /// </summary>
    private static async ValueTask<IReadOnlyDictionary<string, ToolApprovalRequestContent>?> ResolvePendingApprovalCallsAsync(
        AIConversationHistoryBinding binding,
        AGUIRunRequest request,
        CancellationToken cancellationToken)
    {
        if (binding.ResolveApprovalToolCalls is null || request.Resume is not { Count: > 0 })
        {
            return null;
        }

        var callIds = request.Resume
            .Where(e => AGUI.AGUIInterruptKind.IsApproval(e.InterruptId))
            .Select(e => AGUI.AGUIInterruptKind.GetCallId(e.InterruptId))
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return callIds.Count == 0
            ? null
            : await binding.ResolveApprovalToolCalls(callIds, cancellationToken);
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
            additionalProperties: BuildAdditionalPropertiesFromOptions(options),
            approvalPolicy: options.ApprovalPolicy,
            cancellationToken);

        if (context is null)
        {
            throw new InvalidOperationException("Agent execution cancelled by notification handler.");
        }

        bool isSuccess = false;
        string? responseText = null;
        Exception? capturedException = null;
        try
        {
            var response = await context.MafAgent.RunAsync(chatMessages, session: null, options: null, cancellationToken);
            responseText = response.Text;
            isSuccess = true;
            return response;
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw;
        }
        finally
        {
            await PublishExecutedNotificationAsync(context, isSuccess, responseText, capturedException);
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
            additionalProperties: BuildAdditionalPropertiesFromOptions(options),
            approvalPolicy: options.ApprovalPolicy,
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
        AIApprovalPolicy approvalPolicy,
        CancellationToken cancellationToken)
    {
        // Apply profile override if specified
        if (options.ProfileIdOverride.HasValue)
        {
            agent.ProfileId = options.ProfileIdOverride.Value;
        }

        // Publish executing notification (before execution)
        var eventMessages = new EventMessages();
        var executingNotification = new AIAgentExecutingNotification(agent, chatMessages, eventMessages)
        {
            ConversationId = options.ConversationHistory?.ConversationId,
        };
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
                    frontendTool.IsDestructive,
                    _loggerFactory);

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

        // Hand the factory the permission decision we just made, so the server-side tool list is
        // built from the same allowed set as the frontend tools above. The factory cannot resolve
        // this itself without losing options.UserGroupIds, and recomputing from the agent's own
        // defaults would drop every per-user-group allow and deny.
        additionalProperties[AgentConstants.ContextKeys.AllowedToolIds] = allowedToolIds;

        if (options.ContextIdsOverride is not null)
        {
            additionalProperties[CoreConstants.ContextKeys.ContextIdsOverride] = options.ContextIdsOverride;
        }

        if (options.GuardrailIdsOverride is not null)
        {
            additionalProperties[AI.Core.Constants.ContextKeys.GuardrailIdsOverride] = options.GuardrailIdsOverride;
        }

        // Surface the bound conversation id into the runtime context (visible to chat middleware /
        // telemetry) and persist it onto the audit log alongside any keys the caller already flagged.
        if (options.ConversationHistory is { } historyBinding)
        {
            additionalProperties[Constants.ContextKeys.ConversationId] = historyBinding.ConversationId;
            additionalProperties[CoreConstants.ContextKeys.LogKeys] = AppendLogKey(
                additionalProperties.GetValueOrDefault(CoreConstants.ContextKeys.LogKeys),
                Constants.ContextKeys.ConversationId);

            // We manage history via the attached provider, so providers must not also persist it
            // server-side (that conflict otherwise detaches our provider — see the OpenAI provider).
            additionalProperties[CoreConstants.ContextKeys.ClientManagedChatHistory] = true;
        }

        // Create MAF agent. The AG-UI streaming caller passes Interactive (it can resume), while
        // headless callers pass options.ApprovalPolicy (default DenyAll) so destructive tools
        // never stall a run that has no way to approve them. Only the persisted path uses the
        // history-provider overload; every other caller takes the original overload unchanged.
        var mafAgent = options.ConversationHistory is { } binding
            ? await _agentFactory.CreateAgentAsync(
                agent, binding.Provider, contextItems, convertedFrontendTools, additionalProperties, approvalPolicy, cancellationToken)
            : await _agentFactory.CreateAgentAsync(
                agent, contextItems, convertedFrontendTools, additionalProperties, approvalPolicy, cancellationToken);

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
    /// Publishes the executed notification with duration, success status, and optional
    /// response text / captured exception for non-streaming callers.
    /// </summary>
    private async Task PublishExecutedNotificationAsync(
        AgentExecutionContext context,
        bool isSuccess,
        string? responseText = null,
        Exception? exception = null)
    {
        var executedNotification = new AIAgentExecutedNotification(
            context.Agent,
            context.ChatMessages,
            context.Stopwatch.Elapsed,
            isSuccess,
            context.EventMessages)
            {
                ResponseText = responseText,
                Exception = exception,
                ConversationId = context.ExecutingNotification.ConversationId,
            }
            .WithStateFrom(context.ExecutingNotification);

        await _eventAggregator.PublishAsync(executedNotification);
    }

    /// <summary>
    /// Emits an AG-UI error sequence: run started, then run error.
    /// Per spec a run terminates with either RUN_FINISHED or RUN_ERROR — never both.
    /// </summary>
    private static async IAsyncEnumerable<IAGUIEvent> EmitAGUIError(
        AGUIRunRequest request,
        string message,
        string code)
    {
        var emitter = new AGUIEventEmitter(request.ThreadId, request.RunId);
        yield return emitter.EmitRunStarted();
        yield return emitter.EmitError(message, code);
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
