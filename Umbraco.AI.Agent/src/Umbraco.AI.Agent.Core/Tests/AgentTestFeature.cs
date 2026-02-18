using System.Diagnostics;
using System.Text.Json;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Events.Messages;
using Umbraco.AI.AGUI.Events.Tools;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Test feature for testing AI agents.
/// Executes agents with messages, tools, and context, and captures the AG-UI event stream.
/// </summary>
[AITestFeature("agent", "Agent Test", Category = "Built-in")]
public class AgentTestFeature : AITestFeatureBase
{
    private readonly IAIAgentService _agentService;

    /// <inheritdoc />
    public override string Description => "Tests agent execution with messages, tools, and context";

    /// <inheritdoc />
    public override Type? TestCaseType => typeof(AgentTestTestCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTestFeature"/> class.
    /// </summary>
    public AgentTestFeature(
        IAIAgentService agentService,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _agentService = agentService;
    }

    /// <inheritdoc />
    public override async Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        CancellationToken cancellationToken)
    {
        // Deserialize test case from test.TestCaseJson
        var testCase = JsonSerializer.Deserialize<AgentTestTestCase>(test.TestCase.TestCaseJson);
        if (testCase == null)
        {
            throw new InvalidOperationException("Failed to deserialize test case");
        }

        // Resolve target agent (by ID or alias)
        Guid agentId;
        if (test.Target.IsAlias)
        {
            var agent = await _agentService.GetAgentByAliasAsync(test.Target.TargetId, cancellationToken);
            if (agent == null)
            {
                throw new InvalidOperationException($"Agent with alias '{test.Target.TargetId}' not found");
            }
            agentId = agent.Id;
        }
        else
        {
            agentId = Guid.Parse(test.Target.TargetId);
        }

        // Build AG-UI run request
        var request = new AGUIRunRequest
        {
            ThreadId = testCase.ThreadId ?? test.Id.ToString(),
            RunId = $"{test.Id}-run-{runNumber}",
            Messages = testCase.Messages,
            Tools = testCase.Tools,
            State = testCase.State,
            Context = testCase.Context
        };

        // Execute agent and capture timing
        var stopwatch = Stopwatch.StartNew();

        var messages = new List<object>();
        var toolCalls = new List<object>();
        var reasoning = new List<object>();
        string? error = null;
        string? outcome = null;
        string? finalContent = null;
        var currentMessageContent = new List<string>();
        string? currentMessageId = null;

        // Convert AGUITools to AIFrontendTools (with default metadata for testing)
        IEnumerable<AIFrontendTool>? frontendTools = testCase.Tools?
            .Select(t => new AIFrontendTool(t, Scope: null, IsDestructive: false))
            .ToList();

        try
        {
            // TODO: Handle profileIdOverride and contextIdsOverride once agent service supports it
            await foreach (var evt in _agentService.StreamAgentAsync(agentId, request, frontendTools, cancellationToken))
            {
                // Track lifecycle events
                if (evt is RunStartedEvent runStarted)
                {
                    messages.Add(new { role = "system", content = $"Run started: {runStarted.RunId}" });
                }
                else if (evt is RunFinishedEvent runFinished)
                {
                    outcome = runFinished.Outcome.ToString().ToLowerInvariant();
                    messages.Add(new { role = "system", content = $"Run finished: {runFinished.Outcome}" });
                }
                else if (evt is RunErrorEvent runError)
                {
                    error = runError.Message;
                    messages.Add(new { role = "error", content = runError.Message });
                }
                // Track message events
                else if (evt is TextMessageStartEvent msgStart)
                {
                    currentMessageId = msgStart.MessageId;
                    currentMessageContent.Clear();
                }
                else if (evt is TextMessageChunkEvent msgChunk)
                {
                    if (msgChunk.MessageId == currentMessageId)
                    {
                        currentMessageContent.Add(msgChunk.Delta ?? string.Empty);
                    }
                }
                else if (evt is TextMessageContentEvent msgContent)
                {
                    currentMessageContent.Add(msgContent.Delta ?? string.Empty);
                }
                else if (evt is TextMessageEndEvent msgEnd)
                {
                    if (msgEnd.MessageId == currentMessageId)
                    {
                        var content = string.Concat(currentMessageContent);
                        messages.Add(new { role = "assistant", content });
                        finalContent = content;
                        currentMessageContent.Clear();
                        currentMessageId = null;
                    }
                }
                // Track tool call events
                else if (evt is ToolCallStartEvent toolStart)
                {
                    toolCalls.Add(new
                    {
                        id = toolStart.ToolCallId,
                        name = toolStart.ToolCallName,
                        status = "started"
                    });
                }
                else if (evt is ToolCallArgsEvent toolArgs)
                {
                    toolCalls.Add(new
                    {
                        id = toolArgs.ToolCallId,
                        args = toolArgs.Delta,
                        status = "args_received"
                    });
                }
                else if (evt is ToolCallEndEvent toolEnd)
                {
                    toolCalls.Add(new
                    {
                        id = toolEnd.ToolCallId,
                        status = "completed"
                    });
                }
                else if (evt is ToolCallResultEvent toolResult)
                {
                    toolCalls.Add(new
                    {
                        id = toolResult.ToolCallId,
                        result = toolResult.Content,
                        status = "result_received"
                    });
                }
                // Track step events for reasoning
                else if (evt is StepStartedEvent stepStart)
                {
                    reasoning.Add(new
                    {
                        type = "step_started",
                        stepName = stepStart.StepName
                    });
                }
                else if (evt is StepFinishedEvent stepFinished)
                {
                    reasoning.Add(new
                    {
                        type = "step_finished",
                        stepName = stepFinished.StepName
                    });
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Return transcript with error details
            return new AITestTranscript
            {
                RunId = Guid.NewGuid(), // Will be set by the runner
                MessagesJson = JsonSerializer.Serialize(new[]
                {
                    new { role = "system", content = "Agent execution failed" },
                    new { role = "error", content = ex.Message }
                }),
                FinalOutputJson = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                }),
                TimingJson = JsonSerializer.Serialize(new
                {
                    totalMs = stopwatch.ElapsedMilliseconds,
                    status = "error"
                })
            };
        }

        stopwatch.Stop();

        // Build structured transcript
        return new AITestTranscript
        {
            RunId = Guid.NewGuid(), // Will be set by the runner
            MessagesJson = JsonSerializer.Serialize(messages),
            ToolCallsJson = toolCalls.Count > 0 ? JsonSerializer.Serialize(toolCalls) : null,
            ReasoningJson = reasoning.Count > 0 ? JsonSerializer.Serialize(reasoning) : null,
            TimingJson = JsonSerializer.Serialize(new
            {
                totalMs = stopwatch.ElapsedMilliseconds,
                status = outcome ?? "success"
            }),
            FinalOutputJson = JsonSerializer.Serialize(new
            {
                content = finalContent,
                outcome,
                error,
                toolCallCount = toolCalls.Count,
                messageCount = messages.Count
            })
        };
    }
}
