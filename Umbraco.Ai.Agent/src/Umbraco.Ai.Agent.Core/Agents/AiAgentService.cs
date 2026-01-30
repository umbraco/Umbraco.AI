using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Versioning;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;

using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Service implementation for agent management operations.
/// </summary>
internal sealed class AiAgentService : IAiAgentService
{
    private readonly IAiAgentRepository _repository;
    private readonly IAiEntityVersionService _versionService;
    private readonly IAiAgentFactory _agentFactory;
    private readonly IAguiStreamingService _streamingService;
    private readonly IAguiToolConverter _toolConverter;
    private readonly IAguiContextConverter _contextConverter;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AiAgentService(
        IAiAgentRepository repository,
        IAiEntityVersionService versionService,
        IAiAgentFactory agentFactory,
        IAguiStreamingService streamingService,
        IAguiToolConverter toolConverter,
        IAguiContextConverter contextConverter,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _repository = repository;
        _versionService = versionService;
        _agentFactory = agentFactory;
        _streamingService = streamingService;
        _toolConverter = toolConverter;
        _contextConverter = contextConverter;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    /// <inheritdoc />
    public Task<AiAgent?> GetAgentAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<AiAgent?> GetAgentByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => _repository.GetByAliasAsync(alias, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    /// <inheritdoc />
    public Task<PagedModel<AiAgent>> GetAgentsPagedAsync(
        int skip,
        int take,
        string? filter = null,
        Guid? profileId = null,
        string? scopeId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, scopeId, isActive, cancellationToken);

    /// <inheritdoc />
    public Task<IEnumerable<AiAgent>> GetAgentsByScopeAsync(string scopeId, CancellationToken cancellationToken = default)
        => _repository.GetByScopeAsync(scopeId, cancellationToken);

    /// <inheritdoc />
    public async Task<AiAgent> SaveAgentAsync(AiAgent agent, CancellationToken cancellationToken = default)
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
    public async IAsyncEnumerable<IAguiEvent> StreamAgentAsync(
        Guid agentId,
        AguiRunRequest request,
        IEnumerable<AguiTool>? frontendToolDefinitions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. Resolve agent
        var agent = await GetAgentAsync(agentId, cancellationToken);

        if (agent is null)
        {
            // Emit error event and finish
            var errorEmitter = new AguiEventEmitter(request.ThreadId, request.RunId);
            yield return errorEmitter.EmitRunStarted();
            yield return errorEmitter.EmitError("Agent not found", "NOT_FOUND");
            yield return errorEmitter.EmitRunFinished(new InvalidOperationException("Agent not found"));
            yield break;
        }

        if (!agent.IsActive)
        {
            var errorEmitter = new AguiEventEmitter(request.ThreadId, request.RunId);
            yield return errorEmitter.EmitRunStarted();
            yield return errorEmitter.EmitError($"Agent '{agent.Name}' is not active", "AGENT_NOT_ACTIVE");
            yield return errorEmitter.EmitRunFinished(new InvalidOperationException($"Agent '{agent.Name}' is not active"));
            yield break;
        }

        // 2. Convert AG-UI context and frontend tools
        var contextItems = _contextConverter.ConvertToRequestContextItems(request.Context);
        var frontendTools = _toolConverter.ConvertToFrontendTools(frontendToolDefinitions);

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
            frontendTools,
            additionalProperties,
            cancellationToken);

        // 5. Stream via AG-UI streaming service
        //    No additionalSystemPrompt needed - ScopedAIAgent handles it
        await foreach (var evt in _streamingService.StreamAgentAsync(agentInst, request, frontendTools, cancellationToken))
        {
            yield return evt;
        }
    }
}
