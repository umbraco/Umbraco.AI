using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Models;
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

    public AIAgentService(
        IAIAgentRepository repository,
        IAIEntityVersionService versionService,
        IAIAgentFactory agentFactory,
        IAGUIStreamingService streamingService,
        IAGUIContextConverter contextConverter,
        AIToolCollection toolCollection,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _agentFactory = agentFactory;
        _streamingService = streamingService;
        _contextConverter = contextConverter;
        _toolCollection = toolCollection;
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
        string? scopeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, scopeId, isActive, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AIAgent>> GetAgentsByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
        => _repository.GetByScopeAsync(scopeId, cancellationToken);

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

        // Save version snapshot of existing entity before update
        var existing = await _repository.GetByIdAsync(agent.Id, cancellationToken);
        if (existing is not null)
        {
            await _versionService.SaveVersionAsync(existing, userId, null, cancellationToken);
        }

        return await _repository.SaveAsync(agent, userId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAgentAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

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

        // 5. Stream via AG-UI streaming service
        //    No additionalSystemPrompt needed - ScopedAIAgent handles it
        await foreach (var evt in _streamingService.StreamAgentAsync(agentInst, request, convertedFrontendTools, cancellationToken))
        {
            yield return evt;
        }
    }
}
