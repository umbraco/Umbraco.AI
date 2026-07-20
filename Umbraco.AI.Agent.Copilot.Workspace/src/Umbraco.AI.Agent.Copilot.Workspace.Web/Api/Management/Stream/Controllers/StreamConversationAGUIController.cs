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
            session => _historyProvider.BindConversation(session, id));

        var options = new AIAgentExecutionOptions
        {
            ConversationHistory = binding,
            AdditionalProperties = await BuildProjectContextAsync(conversation.ProjectId, cancellationToken),
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

        var lastUserMessage = request.Messages?
            .LastOrDefault(m => m.Role == AGUIMessageRole.User)?.Content ?? string.Empty;

        var selected = await _agentService.SelectAgentForPromptAsync(
            lastUserMessage,
            CopilotWorkspaceAgentSurface.SurfaceId,
            new AgentAvailabilityContext { Surface = CopilotWorkspaceAgentSurface.SurfaceId },
            cancellationToken);

        return selected?.Id;
    }

    /// <summary>
    /// Builds the runtime-context properties that inject a project's context set into the run: its
    /// referenced <c>AIContext</c> ids and its directly-attached resources. Returns null when the
    /// conversation belongs to no project (or the project is unavailable), leaving resolution untouched.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, object?>?> BuildProjectContextAsync(
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        if (projectId is null)
        {
            return null;
        }

        var project = await _projectService.GetProjectAsync(projectId.Value, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var properties = new Dictionary<string, object?>();

        if (project.ContextIds.Count > 0)
        {
            // Honoured by ProfileContextResolver (the "attach a context" mechanism).
            properties[CoreConstants.ContextKeys.AdditionalContextIds] = project.ContextIds.ToList();
        }

        if (project.Resources.Count > 0)
        {
            // Honoured by AdditionalResourcesContextResolver (S3 — the "attach a resource" mechanism).
            properties[CoreConstants.ContextKeys.AdditionalResources] = project.Resources
                .OrderBy(r => r.SortOrder)
                .Select(r => new AIContextResolverResource
                {
                    Id = r.Id,
                    ResourceTypeId = r.ResourceTypeId,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    Settings = r.Settings,
                    InjectionMode = r.InjectionMode,
                    ContextName = project.Name,
                })
                .ToList();
        }

        return properties.Count > 0 ? properties : null;
    }
}
