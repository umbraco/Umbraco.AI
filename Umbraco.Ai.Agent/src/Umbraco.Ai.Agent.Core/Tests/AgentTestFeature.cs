using System.Diagnostics;
using System.Text.Json;
using Umbraco.Ai.Agui.Events;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Events.Tools;
using Umbraco.Ai.Agui.Models;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Tests;

namespace Umbraco.Ai.Agent.Core.Tests;

/// <summary>
/// Test feature for testing agent execution.
/// Executes agents with mock or real context and captures transcripts.
/// </summary>
[AiTestFeature("agent", "Agent Test", Category = "Built-in")]
public class AgentTestFeature : IAiTestFeature
{
    private readonly IAiAgentService _agentService;
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;

    /// <inheritdoc />
    public string Id => "agent";

    /// <inheritdoc />
    public string Name => "Agent Test";

    /// <inheritdoc />
    public string Description => "Tests agent execution with messages and tools";

    /// <inheritdoc />
    public string Category => "Built-in";

    /// <inheritdoc />
    public Type? TestCaseType => typeof(AgentTestTestCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTestFeature"/> class.
    /// </summary>
    public AgentTestFeature(
        IAiAgentService agentService,
        IAiEditableModelSchemaBuilder schemaBuilder)
    {
        _agentService = agentService;
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public AiEditableModelSchema? GetTestCaseSchema()
    {
        return _schemaBuilder.BuildForType<AgentTestTestCase>(Id);
    }

    /// <inheritdoc />
    public async Task<AiTestTranscript> ExecuteAsync(
        AiTest test,
        int runNumber,
        CancellationToken cancellationToken = default)
    {
        // Deserialize test case from test.TestCase.TestCaseJson
        var testCase = JsonSerializer.Deserialize<AgentTestTestCase>(test.TestCase.TestCaseJson)
            ?? throw new InvalidOperationException("Failed to deserialize test case JSON");

        // Resolve target agent from test
        var agentId = await ResolveAgentIdAsync(test.Target, cancellationToken);

        // Build AG-UI request
        var request = new AguiRunRequest
        {
            ThreadId = Guid.NewGuid().ToString(),
            RunId = Guid.NewGuid().ToString(),
            Messages = JsonSerializer.Deserialize<IEnumerable<AguiMessage>>(testCase.MessagesJson)
                ?? throw new InvalidOperationException("Failed to deserialize messages JSON"),
            Tools = testCase.ToolsJson != null
                ? JsonSerializer.Deserialize<IEnumerable<AguiTool>>(testCase.ToolsJson)
                : null,
            State = testCase.StateJson != null
                ? JsonSerializer.Deserialize<JsonElement>(testCase.StateJson)
                : null,
            Context = ParseContextItems(testCase.ContextItemsJson),
        };

        // Execute agent and collect events
        var events = new List<IAguiEvent>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await foreach (var aguiEvent in _agentService.StreamAgentAsync(
                agentId,
                request,
                request.Tools,
                cancellationToken))
            {
                events.Add(aguiEvent);
            }
        }
        catch (Exception ex)
        {
            // Capture execution errors in transcript
            stopwatch.Stop();
            return new AiTestTranscript
            {
                MessagesJson = JsonSerializer.Serialize(new[]
                {
                    new { role = "system", content = "Agent execution failed" },
                }),
                FinalOutputJson = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    exception = ex.GetType().Name,
                }),
                TimingJson = JsonSerializer.Serialize(new
                {
                    totalMs = stopwatch.ElapsedMilliseconds,
                    failed = true,
                }),
            };
        }

        stopwatch.Stop();

        // Extract structured data from events
        var messages = ExtractMessages(events);
        var toolCalls = ExtractToolCalls(events);
        var reasoning = ExtractReasoning(events);
        var timing = ExtractTiming(events, stopwatch.ElapsedMilliseconds);

        // Return structured transcript
        return new AiTestTranscript
        {
            MessagesJson = JsonSerializer.Serialize(messages),
            ToolCallsJson = JsonSerializer.Serialize(toolCalls),
            ReasoningJson = JsonSerializer.Serialize(reasoning),
            FinalOutputJson = JsonSerializer.Serialize(new
            {
                events,
                threadId = request.ThreadId,
                runId = request.RunId,
            }),
            TimingJson = JsonSerializer.Serialize(timing),
        };
    }

    /// <summary>
    /// Resolves the agent ID from the test target.
    /// </summary>
    private async Task<Guid> ResolveAgentIdAsync(AiTestTarget target, CancellationToken cancellationToken)
    {
        if (target.IsAlias)
        {
            var agent = await _agentService.GetAgentByAliasAsync(target.TargetId, cancellationToken);
            if (agent == null)
            {
                throw new InvalidOperationException($"Agent with alias '{target.TargetId}' not found");
            }
            return agent.Id;
        }

        if (Guid.TryParse(target.TargetId, out var agentId))
        {
            return agentId;
        }

        throw new InvalidOperationException($"Invalid agent target ID: '{target.TargetId}'");
    }

    /// <summary>
    /// Parses context items from JSON string.
    /// </summary>
    private static IEnumerable<AguiContextItem>? ParseContextItems(string? contextItemsJson)
    {
        if (string.IsNullOrWhiteSpace(contextItemsJson))
        {
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<ContextItemDto>>(contextItemsJson);
            return items?.Select(x => new AguiContextItem
            {
                Description = x.Description ?? string.Empty,
                Value = x.Value,
            }).ToList();
        }
        catch
        {
            // If parsing fails, return null (invalid JSON)
            return null;
        }
    }

    /// <summary>
    /// Extracts message events from the event stream.
    /// </summary>
    private static List<object> ExtractMessages(List<IAguiEvent> events)
    {
        var messages = new List<object>();

        // Collect all message-related events
        foreach (var evt in events)
        {
            if (evt.GetType().Name.Contains("Message"))
            {
                messages.Add(new
                {
                    type = evt.GetType().Name,
                    timestamp = (evt as BaseAguiEvent)?.Timestamp,
                    data = evt,
                });
            }
        }

        return messages;
    }

    /// <summary>
    /// Extracts tool call events from the event stream.
    /// </summary>
    private static List<object> ExtractToolCalls(List<IAguiEvent> events)
    {
        var toolCalls = new List<object>();

        // Collect all tool call-related events
        foreach (var evt in events)
        {
            if (evt.GetType().Name.Contains("ToolCall"))
            {
                toolCalls.Add(new
                {
                    type = evt.GetType().Name,
                    timestamp = (evt as BaseAguiEvent)?.Timestamp,
                    data = evt,
                });
            }
        }

        return toolCalls;
    }

    /// <summary>
    /// Extracts reasoning/thinking events from the event stream.
    /// Note: AG-UI doesn't have explicit thinking events, so we collect step metadata instead.
    /// </summary>
    private static List<object> ExtractReasoning(List<IAguiEvent> events)
    {
        var reasoning = new List<object>();

        // Collect any custom events or step information that might contain reasoning
        foreach (var evt in events)
        {
            if (evt.GetType().Name.Contains("Step"))
            {
                reasoning.Add(new
                {
                    type = evt.GetType().Name,
                    timestamp = (evt as BaseAguiEvent)?.Timestamp,
                    data = evt,
                });
            }
        }

        return reasoning;
    }

    /// <summary>
    /// Extracts timing information from events and total elapsed time.
    /// </summary>
    private static object ExtractTiming(List<IAguiEvent> events, long totalMs)
    {
        return new
        {
            totalMs,
            steps = events.Select(e => new
            {
                type = e.GetType().Name,
                timestamp = (e as BaseAguiEvent)?.Timestamp,
            }),
        };
    }

    /// <summary>
    /// DTO for deserializing context items from JSON.
    /// </summary>
    private sealed class ContextItemDto
    {
        public string? Description { get; set; }
        public required string Value { get; set; }
    }
}
