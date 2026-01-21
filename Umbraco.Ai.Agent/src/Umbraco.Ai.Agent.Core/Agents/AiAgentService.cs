using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agui;
using Umbraco.Ai.Agent.Core.Chat;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agui.Streaming;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Cms.Core.Models;

using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Service implementation for agent management operations.
/// </summary>
internal sealed class AiAgentService : IAiAgentService
{
    private readonly IAiAgentRepository _repository;
    private readonly IAiAgentFactory _agentFactory;
    private readonly IAguiStreamingService _streamingService;
    private readonly IAguiToolConverter _toolConverter;
    private readonly IAguiContextConverter _contextConverter;
    private readonly IAiRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AiRuntimeContextContributorCollection _contextContributors;

    public AiAgentService(
        IAiAgentRepository repository,
        IAiAgentFactory agentFactory,
        IAguiStreamingService streamingService,
        IAguiToolConverter toolConverter,
        IAguiContextConverter contextConverter,
        IAiRuntimeContextScopeProvider runtimeContextScopeProvider,
        AiRuntimeContextContributorCollection contextContributors)
    {
        _repository = repository;
        _agentFactory = agentFactory;
        _streamingService = streamingService;
        _toolConverter = toolConverter;
        _contextConverter = contextConverter;
        _runtimeContextScopeProvider = runtimeContextScopeProvider;
        _contextContributors = contextContributors;
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
        CancellationToken cancellationToken = default)
        => _repository.GetPagedAsync(skip, take, filter, profileId, cancellationToken);

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

        return await _repository.SaveAsync(agent, cancellationToken);
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
        var contextItems = _contextConverter.ConvertToRuntimeContextItems(request.Context);
        var frontendTools = _toolConverter.ConvertToFrontendTools(frontendToolDefinitions);

        // 3. Create runtime context scope
        using var scope = _runtimeContextScopeProvider.CreateScope(contextItems);
        var runtimeContext = scope.Context;

        // 4. Populate context with contributors
        _contextContributors.Populate(runtimeContext);

        // 5. Build additional properties for auditing
        var additionalProperties = new Dictionary<string, object?>
        {
            { Constants.MetadataKeys.RunId, request.RunId },
            { Constants.MetadataKeys.ThreadId, request.ThreadId },
            { CoreConstants.ContextKeys.LogKeys, new[]
            {
                Constants.MetadataKeys.RunId,
                Constants.MetadataKeys.ThreadId
            }}
        };

        // 6. Create MAF agent (inside scope - context is now valid!)
        var mafAgent = await _agentFactory.CreateAgentAsync(
            agent,
            frontendTools,
            additionalProperties,
            cancellationToken);

        // 7. Build additional system prompt from context contributors
        var additionalSystemPrompt = runtimeContext.SystemMessageParts.Count > 0
            ? string.Join("\n\n", runtimeContext.SystemMessageParts)
            : null;

        // 8. Stream via generic streaming service
        await foreach (var evt in _streamingService.StreamAgentAsync(
            mafAgent, request, frontendTools, additionalSystemPrompt, cancellationToken))
        {
            yield return evt;
        }
    }
}
