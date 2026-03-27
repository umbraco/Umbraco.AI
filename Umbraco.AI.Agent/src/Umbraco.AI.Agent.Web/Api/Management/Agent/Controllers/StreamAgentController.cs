using System.Text.Json;
using Asp.Versioning;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

/// <summary>
/// Controller for streaming agents with M.E.AI types via SSE.
/// </summary>
[ApiVersion("1.0")]
public class StreamAgentController : AgentControllerBase
{
    private readonly IAIAgentService _agentService;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAgentController"/> class.
    /// </summary>
    public StreamAgentController(
        IAIAgentService agentService,
        IUmbracoMapper umbracoMapper)
    {
        _agentService = agentService;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Streams an agent execution and returns updates as SSE.
    /// </summary>
    /// <param name="agentIdOrAlias">The agent ID (GUID) or alias.</param>
    /// <param name="requestModel">The run request containing messages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An SSE stream of agent response updates.</returns>
    [HttpPost($"{{{nameof(agentIdOrAlias)}}}/stream")]
    [MapToApiVersion("1.0")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IResult> StreamAgent(
        IdOrAlias agentIdOrAlias,
        RunAgentRequestModel requestModel,
        CancellationToken cancellationToken = default)
    {
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

        var messages = _umbracoMapper.MapEnumerable<ChatMessageModel, ChatMessage>(requestModel.Messages).ToList();
        var updates = _agentService.StreamAgentAsync(agentId.Value, messages, cancellationToken: cancellationToken);

        return new AgentStreamResult(updates);
    }

    /// <summary>
    /// IResult that streams <see cref="AgentResponseUpdate"/> items as SSE data lines.
    /// </summary>
    private sealed class AgentStreamResult : IResult
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IAsyncEnumerable<AgentResponseUpdate> _updates;

        public AgentStreamResult(IAsyncEnumerable<AgentResponseUpdate> updates)
        {
            _updates = updates;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var response = httpContext.Response;
            var ct = httpContext.RequestAborted;

            response.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";
            response.Headers.Connection = "keep-alive";

            var bufferingFeature = httpContext.Features.Get<IHttpResponseBodyFeature>();
            bufferingFeature?.DisableBuffering();

            await using var writer = new StreamWriter(response.Body, leaveOpen: true);

            try
            {
                await foreach (var update in _updates.WithCancellation(ct))
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    var json = JsonSerializer.Serialize(update, SerializerOptions);
                    await writer.WriteAsync($"data: {json}\n\n");
                    await writer.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (InvalidOperationException ex)
            {
                // Agent not found, inactive, or execution cancelled
                var errorJson = JsonSerializer.Serialize(new { error = ex.Message }, SerializerOptions);
                await writer.WriteAsync($"data: {errorJson}\n\n");
                await writer.FlushAsync(ct);
            }
        }
    }
}
