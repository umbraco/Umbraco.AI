using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// A composite MAF agent that executes an orchestration pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Runs each step in the pipeline sequentially, passing the output of each step
/// as additional context to the next. Agent steps delegate to their MAF agent,
/// while Function/Router/Aggregator/Manager steps are handled inline.
/// </para>
/// <para>
/// For the initial implementation, steps are executed sequentially.
/// Concurrent execution (fan-out/fan-in) will be added in a future iteration.
/// </para>
/// </remarks>
internal sealed class OrchestrationPipelineAgent : MsAIAgent
{
    private readonly IReadOnlyList<OrchestrationStep> _steps;

    public OrchestrationPipelineAgent(
        string? name,
        string? description,
        IReadOnlyList<OrchestrationStep> steps)
    {
        Name = name;
        Description = description;
        _steps = steps;
    }

    public override string? Name { get; }
    public override string? Description { get; }

    /// <inheritdoc />
    public override ValueTask<AgentSession> GetNewSessionAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(new OrchestrationPipelineSession());

    /// <inheritdoc />
    public override ValueTask<AgentSession> DeserializeSessionAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<AgentSession>(
            JsonSerializer.Deserialize<OrchestrationPipelineSession>(serializedSession, jsonSerializerOptions)
            ?? new OrchestrationPipelineSession());

    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        AgentResponse? lastResponse = null;

        foreach (var step in _steps)
        {
            if (step.StepType == OrchestrationStepType.End)
                break;

            if (step.Agent is not null && step.StepType == OrchestrationStepType.Agent)
            {
                lastResponse = await step.Agent.RunAsync(messagesList, session, options, cancellationToken);

                // Append agent response as context for next step
                if (lastResponse?.Messages is not null)
                {
                    messagesList.AddRange(lastResponse.Messages);
                }
            }
            // Other step types (Function, Router, Aggregator, Manager) will be
            // implemented in future iterations. For now, they are no-ops.
        }

        return lastResponse ?? new AgentResponse { Messages = [] };
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();

        foreach (var step in _steps)
        {
            if (step.StepType == OrchestrationStepType.End)
                break;

            if (step.Agent is not null && step.StepType == OrchestrationStepType.Agent)
            {
                await foreach (var update in step.Agent.RunStreamingAsync(messagesList, session, options, cancellationToken))
                {
                    yield return update;
                }
            }
        }
    }
}
