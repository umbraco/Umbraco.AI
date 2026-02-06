using System.Text.Json;
using System.Text.Json.Serialization;

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.AGUI.Streaming;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for running agents with AG-UI streaming support.
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
/// The controller delegates to <see cref="IAIAgentService.StreamAgentAsync"/> which
/// orchestrates the complete agent lifecycle including runtime context scope creation.
/// </para>
/// </remarks>
[ApiVersion("1.0")]
public class RunAgentController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunAgentController"/> class.
    /// </summary>
    public RunAgentController(IAIAgentService agentService)
    {
        _agentService = agentService;
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
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/run")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> RunAgent(
        IdOrAlias agentIdOrAlias,
        AGUIRunRequest request,
        CancellationToken cancellationToken = default)
    {
        // Resolve agent ID from ID or alias
        var agentId = await _agentService.TryGetAgentIdAsync(agentIdOrAlias, cancellationToken);
        if (agentId is null)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "AIAgent not found",
                Detail = "The specified agent could not be found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // Extract tool metadata from ForwardedProps and rejoin with tools
        var frontendTools = CombineToolsWithMetadata(request.Tools, request.ForwardedProps);

        // Delegate to service - handles tool creation, permission filtering, and streaming
        var events = _agentService.StreamAgentAsync(
            agentId.Value,
            request,
            frontendTools,
            cancellationToken);

        return new AGUIEventStreamResult(events);
    }

    /// <summary>
    /// Combines AG-UI tools with their metadata from ForwardedProps.
    /// Extracts tool metadata (scope, isDestructive) from forwardedProps and rejoins with tool definitions.
    /// </summary>
    /// <param name="tools">AG-UI tool definitions from the request.</param>
    /// <param name="forwardedProps">Forwarded properties containing tool metadata.</param>
    /// <returns>List of frontend tools with metadata attached, or null if no tools provided.</returns>
    private IEnumerable<AIFrontendTool>? CombineToolsWithMetadata(
        IEnumerable<AGUITool>? tools,
        JsonElement? forwardedProps)
    {
        if (tools is null)
        {
            return null;
        }

        // Extract metadata lookup
        var metadataLookup = ExtractToolMetadataLookup(forwardedProps);

        // Combine tools with their metadata
        var frontendTools = new List<AIFrontendTool>();
        foreach (var tool in tools)
        {
            // Get metadata for this tool (if available)
            string? scope = null;
            bool isDestructive = false;

            if (metadataLookup.TryGetValue(tool.Name, out var metadata))
            {
                scope = metadata.Scope;
                isDestructive = metadata.IsDestructive;
            }

            frontendTools.Add(new AIFrontendTool(tool, scope, isDestructive));
        }

        return frontendTools;
    }

    /// <summary>
    /// Extracts tool metadata from ForwardedProps into a lookup dictionary.
    /// </summary>
    private Dictionary<string, ToolMetadataDto> ExtractToolMetadataLookup(JsonElement? forwardedProps)
    {
        if (forwardedProps is null ||
            !forwardedProps.Value.TryGetProperty("toolMetadata", out var metadataElement))
        {
            return new Dictionary<string, ToolMetadataDto>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var metadataList = JsonSerializer.Deserialize<List<ToolMetadataDto>>(
                metadataElement.GetRawText()) ?? [];

            return metadataList.ToDictionary(
                m => m.ToolName,
                m => m,
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ToolMetadataDto>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private class ToolMetadataDto
    {
        [JsonPropertyName("toolName")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("isDestructive")]
        public bool IsDestructive { get; set; }
    }
}
