using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Surfaces;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Agents;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agents.Selection;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.Cms.Core.DependencyInjection;
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
    private readonly IAIAgentSelectionService _selectionService;
    private readonly IAGUIMessageConverter _messageConverter;
    private readonly IAGUIToolConverter _toolConverter;
    private readonly ConversationChatHistoryProvider _historyProvider;
    private readonly AIAgentScopeValidator _scopeValidator;
    private readonly AIAgentSurfaceCollection _surfaces;

    /// <summary>Initializes a new instance of the <see cref="StreamConversationAGUIController"/> class.</summary>
    [Obsolete("Use the constructor that accepts an IAIAgentSelectionService and IAGUIMessageConverter so that 'auto' conversations run through the pluggable selector chain. Will be removed in v20.")]
    public StreamConversationAGUIController(
        IAIConversationService conversationService,
        IAIProjectService projectService,
        IAIAgentService agentService,
        IAGUIToolConverter toolConverter,
        ConversationChatHistoryProvider historyProvider)
        : this(
            conversationService,
            projectService,
            agentService,
            StaticServiceProvider.Instance.GetRequiredService<IAIAgentSelectionService>(),
            StaticServiceProvider.Instance.GetRequiredService<IAGUIMessageConverter>(),
            toolConverter,
            historyProvider,
            StaticServiceProvider.Instance.GetRequiredService<AIAgentScopeValidator>(),
            StaticServiceProvider.Instance.GetRequiredService<AIAgentSurfaceCollection>())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StreamConversationAGUIController"/> class.</summary>
    [Obsolete("Use the constructor that accepts an AIAgentScopeValidator and AIAgentSurfaceCollection so that a conversation's named agent is checked against the Copilot Workspace surface. Will be removed in v20.")]
    public StreamConversationAGUIController(
        IAIConversationService conversationService,
        IAIProjectService projectService,
        IAIAgentService agentService,
        IAIAgentSelectionService selectionService,
        IAGUIMessageConverter messageConverter,
        IAGUIToolConverter toolConverter,
        ConversationChatHistoryProvider historyProvider)
        : this(
            conversationService,
            projectService,
            agentService,
            selectionService,
            messageConverter,
            toolConverter,
            historyProvider,
            StaticServiceProvider.Instance.GetRequiredService<AIAgentScopeValidator>(),
            StaticServiceProvider.Instance.GetRequiredService<AIAgentSurfaceCollection>())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StreamConversationAGUIController"/> class.</summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overloads around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public StreamConversationAGUIController(
        IAIConversationService conversationService,
        IAIProjectService projectService,
        IAIAgentService agentService,
        IAIAgentSelectionService selectionService,
        IAGUIMessageConverter messageConverter,
        IAGUIToolConverter toolConverter,
        ConversationChatHistoryProvider historyProvider,
        AIAgentScopeValidator scopeValidator,
        AIAgentSurfaceCollection surfaces)
    {
        _conversationService = conversationService;
        _projectService = projectService;
        _agentService = agentService;
        _selectionService = selectionService;
        _messageConverter = messageConverter;
        _toolConverter = toolConverter;
        _historyProvider = historyProvider;
        _scopeValidator = scopeValidator;
        _surfaces = surfaces;
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
                Type = "Error",
                Title = "Conversation not found",
                Detail = "The specified conversation could not be found for the current user.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        // Tool metadata travels inline via AGUITool.Metadata per AG-UI spec — computed once and reused
        // both for the (possible) auto-selection input and the eventual run.
        var frontendTools = _toolConverter.ConvertToFrontendTools(request.Tools);

        // Resolve the agent (explicit choice stored on the conversation, else auto-select for the
        // Workspace surface). Defined fallback (S10): fail cleanly here rather than mid-stream.
        var explicitAgent = await TryGetExplicitAvailableAgentAsync(conversation.AgentIdOrAlias, cancellationToken);

        Guid agentId;
        AIAgentSelectionResult? selection = null;
        if (explicitAgent is not null)
        {
            agentId = explicitAgent.Id;
        }
        else
        {
            selection = await SelectAgentAsync(conversation, request, frontendTools, cancellationToken);
            if (selection is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Type = "Error",
                    Title = "No agent available",
                    Detail = "No active agent is available for the Copilot Workspace surface.",
                    Status = StatusCodes.Status404NotFound,
                });
            }

            agentId = selection.Agent.Id;
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

            // A refresh/reload before Approve/Deny leaves a dangling approval request in persisted
            // history — recover it so a subsequent non-resume turn can auto-deny it instead of bricking
            // the conversation on FICC's unresolved-approval check.
            ResolveDanglingApprovalRequests = async ct =>
                await _historyProvider.GetDanglingApprovalRequestsAsync(id, ct),

            // A fresh AgentSession is created per HTTP request (below), but session-scoped decorators
            // (e.g. tool-approval-response binding) record their own state directly on the session
            // object rather than in chat history. Restore/persist it explicitly so that state survives
            // across requests the same way the chat messages do.
            LoadSessionState = async ct => await _historyProvider.GetSessionStateAsync(id, ct),
            SaveSessionState = async (state, ct) => await _historyProvider.SaveSessionStateAsync(id, state, ct),

            // A dropped connection can leave the browser's own "already sent" bookkeeping stale, so a
            // plain follow-up turn can resend a leading run of messages this conversation already holds,
            // or the browser can carry on not knowing what actually got saved. PersistenceSync guards
            // both ends of that gap (umbraco/Umbraco.AI#375).
            PersistenceSync = new AIConversationPersistenceSync(
                DropAlreadyPersistedLeadingMessages: async (messages, ct) =>
                    await _historyProvider.DropAlreadyPersistedLeadingMessagesAsync(id, messages, ct),
                ResolveLastPersistedMessageId: async ct =>
                    await _historyProvider.GetLastPersistedMessageIdAsync(id, ct)),
        };

        var options = new AIAgentExecutionOptions
        {
            ConversationHistory = binding,
            AdditionalProperties = await BuildRuntimeContextAsync(conversation, cancellationToken),
            // Only the auto path has a Selection to record - the explicit path (SPEC "Copilot
            // Workspace" 1) keeps running with no selection, like it did before this method existed.
            Selection = selection,
        };

        var events = _agentService.StreamAgentAGUIAsync(agentId, request, frontendTools, options, cancellationToken);

        // Prepend agent_selected when auto mode picked the agent, same as the plain Copilot endpoint.
        if (selection is not null)
        {
            events = AGUIAgentSelectedEvent.Prepend(events, selection, cancellationToken);
        }

        return new AGUIEventStreamResult(events);
    }

    /// <summary>
    /// Resolves the conversation's explicitly stored agent (by id or alias), when it names one that can
    /// still run in Workspace: active, opted in to the <c>copilot-workspace</c> surface, and allowed by its
    /// scope rules. Returns null for an unset/"auto" conversation, or a named agent that is missing or
    /// unavailable - every such case falls back to auto-selection (SPEC "Copilot Workspace" 1-2).
    /// </summary>
    private async Task<AIAgent?> TryGetExplicitAvailableAgentAsync(string? idOrAlias, CancellationToken cancellationToken)
    {
        if (CopilotWorkspaceAgentAvailability.IsAuto(idOrAlias))
        {
            return null;
        }

        var agent = await CopilotWorkspaceAgentAvailability.FindAgentAsync(_agentService, idOrAlias!, cancellationToken);

        return agent is not null && CopilotWorkspaceAgentAvailability.IsAvailable(agent, _scopeValidator, _surfaces)
            ? agent
            : null;
    }

    /// <summary>
    /// Runs the pluggable selector chain (SPEC "Copilot Workspace" 2-3) for an auto conversation: this
    /// turn's converted messages (falling back to the last persisted user message text on a regenerate,
    /// which carries no inbound user message of its own), the frontend tools, and the previous pick -
    /// the agent that produced the conversation's newest assistant message, if any.
    /// </summary>
    private async Task<AIAgentSelectionResult?> SelectAgentAsync(
        AIConversation conversation,
        AGUIRunRequest request,
        IEnumerable<AIFrontendTool>? frontendTools,
        CancellationToken cancellationToken)
    {
        var messages = _messageConverter.ConvertToChatMessages(request.Messages);
        if (!messages.Any(m => m.Role == ChatRole.User && !string.IsNullOrWhiteSpace(m.Text)))
        {
            var lastUserMessage = await _conversationService.GetLastUserMessageTextAsync(conversation.Id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(lastUserMessage))
            {
                messages = [.. messages, new ChatMessage(ChatRole.User, lastUserMessage)];
            }
        }

        var input = new AIAgentSelectionInput
        {
            SurfaceId = CopilotWorkspaceAgentSurface.SurfaceId,
            AvailabilityContext = new AgentAvailabilityContext { Surface = CopilotWorkspaceAgentSurface.SurfaceId },
            Messages = messages,
            // Deliberately empty: Workspace sends no AG-UI request context items - grounding (project/
            // conversation instructions, resources, referenced AIContext ids) reaches the run via
            // AIAgentExecutionOptions.AdditionalProperties (see BuildRuntimeContextAsync), not here.
            ContextItems = [],
            FrontendTools = frontendTools?.ToList() ?? [],
            PreviousAgentId = await _conversationService.GetLastAssistantAgentIdAsync(conversation.Id, cancellationToken),
        };

        return await _selectionService.SelectAgentAsync(input, cancellationToken);
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
