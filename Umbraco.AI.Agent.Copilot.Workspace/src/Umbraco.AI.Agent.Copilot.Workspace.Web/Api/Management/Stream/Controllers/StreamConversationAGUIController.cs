using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Contexts.Resolvers;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream.Controllers;

/// <summary>
/// Streams a persisted Copilot Workspace conversation with AG-UI (SSE), loading and persisting history
/// through the durable conversation store. This is the one endpoint that binds a run to a conversation;
/// ownership is enforced here at the bind point (B7), and persistence is attached via
/// <see cref="AIAgentExecutionOptions.ConversationHistory"/> so the whole run goes through the single
/// <see cref="IAIAgentService"/> assembly path (no duplicated orchestration).
/// </summary>
[ApiVersion("1.0")]
public class StreamConversationAGUIController : CopilotWorkspaceStreamControllerBase
{
    private readonly IAIConversationService _conversationService;
    private readonly IAIProjectService _projectService;
    private readonly IAIAgentService _agentService;
    private readonly IAGUIToolConverter _toolConverter;
    private readonly ConversationChatHistoryProvider _historyProvider;

    /// <summary>Initializes a new instance of the <see cref="StreamConversationAGUIController"/> class.</summary>
    public StreamConversationAGUIController(
        IAIConversationService conversationService,
        IAIProjectService projectService,
        IAIAgentService agentService,
        IAGUIToolConverter toolConverter,
        ConversationChatHistoryProvider historyProvider)
    {
        _conversationService = conversationService;
        _projectService = projectService;
        _agentService = agentService;
        _toolConverter = toolConverter;
        _historyProvider = historyProvider;
    }

    /// <summary>
    /// Runs the conversation's agent with an AG-UI streaming response (SSE), persisting the new turn.
    /// </summary>
    /// <param name="id">The conversation id (also the AG-UI threadId for file scoping).</param>
    /// <param name="request">The AG-UI run request (the new inbound messages, tools, and context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An AG-UI event stream, or a problem response if the conversation/agent is unavailable.</returns>
    [HttpPost("{id:guid}/stream-agui")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> StreamAgentAGUI(
        Guid id,
        AGUIRunRequest request,
        CancellationToken cancellationToken = default)
    {
        // Ownership pinned at the ConversationId bind point (B7). GetConversationAsync is scoped to the
        // acting user, so a conversation the caller does not own is indistinguishable from missing.
        var conversation = await _conversationService.GetConversationAsync(id, cancellationToken);
        if (conversation is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Conversation not found",
                Detail = "The specified conversation could not be found for the current user.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        // Resolve the agent (explicit choice stored on the conversation, else auto-select for the
        // Workspace surface). Defined fallback (S10): fail cleanly here rather than mid-stream.
        var agentId = await ResolveAgentIdAsync(conversation, request, cancellationToken);
        if (agentId is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "No agent available",
                Detail = "No active agent is available for the Copilot Workspace surface.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        // Attach persistence to this run (option A): the concrete session binding travels as a delegate
        // so the Agent layer stays ignorant of the conversation store.
        var binding = new AIConversationHistoryBinding(
            _historyProvider,
            id,
            session => _historyProvider.BindConversation(session, id))
        {
            // On an approval resume after a reload, the original tool call lives only in persisted
            // history — recover it so the run can correlate the approval rather than skip it (B2).
            ResolveApprovalToolCalls = async (callIds, ct) =>
                await _historyProvider.GetApprovalToolCallsAsync(id, callIds, ct),

            // A fresh AgentSession is created per HTTP request (below), but session-scoped decorators
            // (e.g. tool-approval-response binding) record their own state directly on the session
            // object rather than in chat history. Restore/persist it explicitly so that state survives
            // across requests the same way the chat messages do.
            LoadSessionState = async ct => await _historyProvider.GetSessionStateAsync(id, ct),
            SaveSessionState = async (state, ct) => await _historyProvider.SaveSessionStateAsync(id, state, ct),
        };

        var options = new AIAgentExecutionOptions
        {
            ConversationHistory = binding,
            AdditionalProperties = await BuildRuntimeContextAsync(conversation, cancellationToken),
        };

        var frontendTools = _toolConverter.ConvertToFrontendTools(request.Tools);
        var events = _agentService.StreamAgentAGUIAsync(agentId.Value, request, frontendTools, options, cancellationToken);
        return new AGUIEventStreamResult(events);
    }

    /// <summary>
    /// Resolves the agent to run: the explicit agent stored on the conversation (by id or alias) when
    /// active, otherwise an auto-selected agent for the Copilot Workspace surface.
    /// </summary>
    private async Task<Guid?> ResolveAgentIdAsync(
        AIConversation conversation,
        AGUIRunRequest request,
        CancellationToken cancellationToken)
    {
        var idOrAlias = conversation.AgentIdOrAlias;
        if (!string.IsNullOrWhiteSpace(idOrAlias)
            && !string.Equals(idOrAlias, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var agent = Guid.TryParse(idOrAlias, out var explicitId)
                ? await _agentService.GetAgentAsync(explicitId, cancellationToken)
                : await _agentService.GetAgentByAliasAsync(idOrAlias, cancellationToken);

            if (agent is { IsActive: true })
            {
                return agent.Id;
            }
        }

        // A regenerate re-runs the stored turn, so the request carries no inbound user message. Fall back
        // to the persisted one, otherwise auto-selection would be handed an empty prompt and could route
        // the regenerated answer to a different agent than the original.
        var lastUserMessage = request.Messages?
            .LastOrDefault(m => m.Role == AGUIMessageRole.User)?.Content;

        if (string.IsNullOrWhiteSpace(lastUserMessage))
        {
            lastUserMessage = await _conversationService.GetLastUserMessageTextAsync(conversation.Id, cancellationToken);
        }

        var selected = await _agentService.SelectAgentForPromptAsync(
            lastUserMessage ?? string.Empty,
            CopilotWorkspaceAgentSurface.SurfaceId,
            new AgentAvailabilityContext { Surface = CopilotWorkspaceAgentSurface.SurfaceId },
            cancellationToken);

        return selected?.Id;
    }

    /// <summary>
    /// Builds the runtime-context properties injected into the run by stacking two layers: the owning
    /// project's grounding (framing, instructions, resources, referenced <c>AIContext</c> ids — see
    /// <see cref="ProjectRuntimeContextBuilder"/>) and the conversation's <em>own</em> attached
    /// contexts/resources (<see cref="ConversationRuntimeContextBuilder"/>). Returns null when neither
    /// layer contributes anything, leaving resolution untouched.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>?> BuildRuntimeContextAsync(
        AIConversation conversation,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object?>? projectContext = null;
        if (conversation.ProjectId is not null)
        {
            var project = await _projectService.GetProjectAsync(conversation.ProjectId.Value, cancellationToken);
            projectContext = project is null ? null : ProjectRuntimeContextBuilder.Build(project);
        }

        var conversationContext = ConversationRuntimeContextBuilder.Build(conversation);

        return MergeRuntimeContext(projectContext, conversationContext);
    }

    /// <summary>
    /// Merges the project and conversation runtime-context property bags: referenced context ids are
    /// concatenated and de-duplicated (project order first), and resources are appended project-first
    /// so the project's framing/instructions still lead the injected block.
    /// </summary>
    private static IReadOnlyDictionary<string, object?>? MergeRuntimeContext(
        IReadOnlyDictionary<string, object?>? projectContext,
        IReadOnlyDictionary<string, object?>? conversationContext)
    {
        if (projectContext is null)
        {
            return conversationContext;
        }

        if (conversationContext is null)
        {
            return projectContext;
        }

        var merged = new Dictionary<string, object?>(projectContext);

        var ids = new List<Guid>();
        CollectContextIds(projectContext, ids);
        CollectContextIds(conversationContext, ids);
        if (ids.Count > 0)
        {
            merged[CoreConstants.ContextKeys.AdditionalContextIds] = ids.Distinct().ToList();
        }

        var resources = new List<AIContextResolverResource>();
        CollectResources(projectContext, resources);
        CollectResources(conversationContext, resources);
        if (resources.Count > 0)
        {
            merged[CoreConstants.ContextKeys.AdditionalResources] = resources;
        }

        return merged;
    }

    private static void CollectContextIds(IReadOnlyDictionary<string, object?> context, List<Guid> into)
    {
        if (context.TryGetValue(CoreConstants.ContextKeys.AdditionalContextIds, out var value)
            && value is IEnumerable<Guid> ids)
        {
            into.AddRange(ids);
        }
    }

    private static void CollectResources(IReadOnlyDictionary<string, object?> context, List<AIContextResolverResource> into)
    {
        if (context.TryGetValue(CoreConstants.ContextKeys.AdditionalResources, out var value)
            && value is IEnumerable<AIContextResolverResource> resources)
        {
            into.AddRange(resources);
        }
    }
}
