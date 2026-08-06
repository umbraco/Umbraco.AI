using System.Runtime.CompilerServices;

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.AGUI;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Special;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.DependencyInjection;

using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for streaming agents with AG-UI protocol support.
/// </summary>
/// <remarks>
/// <para>
/// This controller uses the Microsoft Agent Framework (MAF) for agent execution,
/// but maintains a custom implementation rather than using MAF's built-in <c>MapAGUI()</c>
/// for the following reasons:
/// </para>
/// <list type="bullet">
///   <item>Frontend tool handling with <c>FunctionInvokingChatClient.CurrentContext.Terminate</c></item>
///   <item>Umbraco authorization/security model integration</item>
///   <item>Custom AG-UI context item handling</item>
/// </list>
/// <para>
/// The controller delegates to <see cref="IAIAgentService.StreamAgentAGUIAsync"/> which
/// orchestrates the complete agent lifecycle including runtime context scope creation.
/// </para>
/// </remarks>
[ApiVersion("1.0")]
public class StreamAgentAGUIController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;
    private readonly IAGUIContextConverter _contextConverter;
    private readonly IAGUIToolConverter _toolConverter;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly AIAgentScopeValidator _scopeValidator;
    private readonly AIAgentSurfaceCollection _surfaceCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAgentAGUIController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an AIAgentScopeValidator and AIAgentSurfaceCollection so that explicitly requested agents are checked against their scope rules. Will be removed in v19.")]
    public StreamAgentAGUIController(
        IAIAgentService agentService,
        IAGUIContextConverter contextConverter,
        IAGUIToolConverter toolConverter,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : this(
            agentService,
            contextConverter,
            toolConverter,
            scopeProvider,
            contributors,
            StaticServiceProvider.Instance.GetRequiredService<AIAgentScopeValidator>(),
            StaticServiceProvider.Instance.GetRequiredService<AIAgentSurfaceCollection>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAgentAGUIController"/> class.
    /// </summary>
    public StreamAgentAGUIController(
        IAIAgentService agentService,
        IAGUIContextConverter contextConverter,
        IAGUIToolConverter toolConverter,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        AIAgentScopeValidator scopeValidator,
        AIAgentSurfaceCollection surfaceCollection)
    {
        _agentService = agentService;
        _contextConverter = contextConverter;
        _toolConverter = toolConverter;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
        _scopeValidator = scopeValidator;
        _surfaceCollection = surfaceCollection;
    }

    /// <summary>
    /// Runs an agent with AG-UI streaming response (SSE).
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="request">The AG-UI run request containing messages and context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of AG-UI events.</returns>
    /// <remarks>
    /// <para>
    /// This endpoint resolves the agent by ID or alias and delegates to
    /// <see cref="IAIAgentService.StreamAgentAsync"/> which handles the full lifecycle:
    /// runtime context creation, MAF agent creation, and AG-UI event streaming.
    /// </para>
    /// <para>
    /// Errors (agent not found, agent not active, profile not found) are returned
    /// as AG-UI events in the stream rather than HTTP error responses, allowing
    /// clients to handle them consistently.
    /// </para>
    /// </remarks>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/stream-agui")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> StreamAgentAGUI(
        IdOrAlias agentIdOrAlias,
        AGUIRunRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid? agentId;
        AIAgent? autoSelectedAgent = null;

        // Handle "auto" alias for automatic agent selection
        if (agentIdOrAlias.IsAlias && string.Equals(agentIdOrAlias.Alias, "auto", StringComparison.OrdinalIgnoreCase))
        {
            // Extract the last user message for classification
            var lastUserMessage = request.Messages?
                .LastOrDefault(m => m.Role ==  AGUIMessageRole.User);

            var userPrompt = lastUserMessage?.Content ?? string.Empty;

            // Build availability context from AG-UI context items
            var context = BuildAvailabilityContext(request.Context);

            if (context.Surface is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Surface is required for auto agent selection",
                    Detail = "The AG-UI context must include a surface to use 'auto' agent selection.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            autoSelectedAgent = await _agentService.SelectAgentForPromptAsync(
                userPrompt, context.Surface, context, cancellationToken);

            if (autoSelectedAgent is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "No active agents found",
                    Detail = $"No active agents found in '{context.Surface}' surface for the current context.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            agentId = autoSelectedAgent.Id;
        }
        else
        {
            // Resolve agent ID from ID or alias
            agentId = await _agentService.TryGetAgentIdAsync(agentIdOrAlias, cancellationToken);
            if (agentId is null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "AIAgent not found",
                    Detail = "The specified agent could not be found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Honour the agent's scope rules on this path too. The auto-selection branch above
            // filters by scope via SelectAgentForPromptAsync, so without this an explicit agent ID
            // was a way to reach an agent the surface had ruled out.
            // Only enforced when the request actually declares a surface: scope rules are
            // surface-relative, so a contextless programmatic caller has nothing to check against
            // and must keep working as before.
            var explicitContext = BuildAvailabilityContext(request.Context);
            if (explicitContext.Surface is not null)
            {
                var agent = await _agentService.GetAgentAsync(agentId.Value, cancellationToken);
                var surface = _surfaceCollection.FirstOrDefault(
                    s => string.Equals(s.Id, explicitContext.Surface, StringComparison.OrdinalIgnoreCase));

                if (agent is not null && !_scopeValidator.IsAgentAvailable(agent, explicitContext, surface))
                {
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "AIAgent not available in this context",
                        Detail = $"Agent '{agent.Alias}' is not available in the '{explicitContext.Surface}' surface for the current context.",
                        Status = StatusCodes.Status404NotFound
                    });
                }
            }
        }

        // Tool metadata travels inline via AGUITool.Metadata per AG-UI spec — no rejoin needed.
        var frontendTools = _toolConverter.ConvertToFrontendTools(request.Tools);

        // Delegate to service - handles tool creation, permission filtering, and streaming
        var events = _agentService.StreamAgentAGUIAsync(
            agentId.Value,
            request,
            frontendTools,
            cancellationToken);

        // Prepend agent_selected event if auto mode was used
        if (autoSelectedAgent is not null)
        {
            events = PrependAgentSelectedEvent(events, autoSelectedAgent, cancellationToken);
        }

        return new AGUIEventStreamResult(events);
    }

    /// <summary>
    /// Prepends an agent_selected custom event to the AG-UI stream.
    /// This informs the frontend which agent was automatically selected in auto mode.
    /// </summary>
    /// <param name="innerStream">The original AG-UI event stream.</param>
    /// <param name="selectedAgent">The agent that was selected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AG-UI event stream with agent_selected event prepended.</returns>
    private static async IAsyncEnumerable<IAGUIEvent> PrependAgentSelectedEvent(
        IAsyncEnumerable<IAGUIEvent> innerStream,
        AIAgent selectedAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new CustomEvent
        {
            Name = "agent_selected",
            Value = new
            {
                agentId = selectedAgent.Id,
                agentName = selectedAgent.Name,
                agentAlias = selectedAgent.Alias
            }
        };

        await foreach (var evt in innerStream.WithCancellation(cancellationToken))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Builds an AgentAvailabilityContext from AG-UI context items using the runtime context infrastructure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates a temporary runtime context scope and processes the context items through contributors
    /// to properly extract section and entity type values. This ensures consistent context extraction
    /// using the same infrastructure as the main agent execution pipeline.
    /// </para>
    /// <para>
    /// Note: This results in context items being processed twice (once for classification, once for
    /// execution), but this overhead is negligible compared to the LLM classification call.
    /// </para>
    /// </remarks>
    /// <param name="contextItems">The AG-UI context items from the request.</param>
    /// <returns>An AgentAvailabilityContext with extracted section and entity type.</returns>
    private AgentAvailabilityContext BuildAvailabilityContext(IEnumerable<AGUIContextItem>? contextItems)
    {
        if (contextItems is null)
        {
            return new AgentAvailabilityContext();
        }

        // Convert AG-UI context items to runtime context items
        var requestContextItems = _contextConverter.ConvertToRequestContextItems(contextItems);

        // Create temporary runtime context scope
        using var scope = _scopeProvider.CreateScope(requestContextItems);

        // Populate the context via contributors (same as ScopedAIAgent does)
        _contributors.Populate(scope.Context);

        // Extract values from the properly populated runtime context
        var surface = scope.Context.GetValue<string>(AgentConstants.ContextKeys.Surface);
        var section = scope.Context.GetValue<string>(CoreConstants.ContextKeys.Section);
        var entityType = scope.Context.GetValue<string>(CoreConstants.ContextKeys.EntityType);

        return new AgentAvailabilityContext
        {
            Surface = surface,
            Section = section,
            EntityType = entityType
        };
    }

}
