using System.Diagnostics;
using System.Text.Json;
using Umbraco.AI.AGUI.Events;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Events.Messages;
using Umbraco.AI.AGUI.Events.Tools;
using Umbraco.AI.AGUI.Models;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Agent.Core.Tests;

/// <summary>
/// Test feature for testing AI agents.
/// Executes agents with messages, tools, and mock entity context, and captures the AG-UI event stream.
/// </summary>
[AITestFeature("agent", "Agent Test", Category = "Built-in")]
public class AgentTestFeature : AITestFeatureBase<AgentTestFeatureConfig>
{
    private readonly IAIAgentService _agentService;
    private readonly IAGUIContextConverter _contextConverter;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <inheritdoc />
    public override string Description => "Tests agent execution with messages, tools, and context";

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTestFeature"/> class.
    /// </summary>
    public AgentTestFeature(
        IAIAgentService agentService,
        IAGUIContextConverter contextConverter,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        AITestContextResolver contextResolver,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(contextResolver, schemaBuilder)
    {
        _agentService = agentService;
        _contextConverter = contextConverter;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    /// <summary>
    /// Flushes any buffered message content into the messages list.
    /// </summary>
    private static void FlushCurrentMessage(
        List<object> messages,
        List<string> currentMessageContent,
        ref string? finalContent,
        ref string? currentMessageId)
    {
        if (currentMessageContent.Count == 0)
        {
            return;
        }

        var content = string.Concat(currentMessageContent);
        messages.Add(new { role = "assistant", content });
        finalContent = content;
        currentMessageContent.Clear();
        currentMessageId = null;
    }

    /// <inheritdoc />
    public override async Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        CancellationToken cancellationToken)
    {
        // Get strongly-typed config
        var config = test.GetTestFeatureConfig<AgentTestFeatureConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize test feature config");
        }

        // Use target agent ID directly (entity picker ensures valid ID)
        Guid agentId = test.TestTargetId;

        // Extract entity context from config
        var entityContext = ResolveEntityContext(config);

        // Mock entity → AG-UI context items
        var resolvedContextItems = ResolveEntityContextItems(config);
        var aguiContextItems = resolvedContextItems
            .Select(item => new AGUIContextItem
            {
                Description = item.Description,
                Value = item.Value
            })
            .ToList();

        // Merge resolved mock entity context with any existing config context
        var mergedContext = aguiContextItems.Count > 0
            ? aguiContextItems
            : null;

        // Build AG-UI run request
        var request = new AGUIRunRequest
        {
            ThreadId = config.ThreadId ?? test.Id.ToString(),
            RunId = $"{test.Id}-run-{runNumber}",
            Messages = [new AGUIMessage { Role = AGUIMessageRole.User, Content = config.Message }],
            Context = mergedContext
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

        // Resolve the full system message (context contributor parts + agent instructions)
        // This mirrors what ScopedAIAgent.InjectSystemMessageParts does at runtime
        var agent = await _agentService.GetAgentAsync(agentId, cancellationToken);
        var requestContextItems = _contextConverter.ConvertToRequestContextItems(mergedContext);
        using (var scope = _scopeProvider.CreateScope(requestContextItems))
        {
            _contributors.Populate(scope.Context);

            var systemParts = new List<string>();
            if (scope.Context.SystemMessageParts.Count > 0)
            {
                systemParts.Add(string.Join("\n\n", scope.Context.SystemMessageParts));
            }

            if (!string.IsNullOrEmpty(agent?.Instructions))
            {
                systemParts.Add(agent.Instructions);
            }

            if (systemParts.Count > 0)
            {
                messages.Add(new { role = "system", content = string.Join("\n\n", systemParts) });
            }
        }

        // Add the user's input message to the transcript
        messages.Add(new { role = "user", content = config.Message });

        // Context IDs → options.ContextIdsOverride (per-run override takes precedence)
        var effectiveContextIds = ResolveEffectiveContextIds(config, contextIdsOverride);

        try
        {
            var options = new AIAgentExecutionOptions
            {
                ProfileIdOverride = profileIdOverride,
                ContextIdsOverride = effectiveContextIds?.ToList()
            };

            await foreach (var evt in _agentService.StreamAgentAsync(agentId, request, null, options, cancellationToken))
            {
                // Track lifecycle events
                if (evt is RunStartedEvent runStarted)
                {
                    messages.Add(new { role = "system", content = $"Run started: {runStarted.RunId}" });
                }
                else if (evt is RunFinishedEvent runFinished)
                {
                    // Flush any buffered message content before finishing
                    FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);

                    outcome = runFinished.Outcome.ToString().ToLowerInvariant();
                    messages.Add(new { role = "system", content = $"Run finished: {runFinished.Outcome}" });
                }
                else if (evt is RunErrorEvent runError)
                {
                    error = runError.Message;
                    messages.Add(new { role = "error", content = runError.Message });
                }
                // Track message events - handle both convenience (chunk-only) and full lifecycle patterns
                else if (evt is TextMessageStartEvent msgStart)
                {
                    // Full lifecycle: flush previous message, start new one
                    FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);
                    currentMessageId = msgStart.MessageId;
                }
                else if (evt is TextMessageChunkEvent msgChunk)
                {
                    // If the message ID changed, flush the previous message and start a new one
                    if (currentMessageId != null && msgChunk.MessageId != currentMessageId)
                    {
                        FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);
                    }

                    currentMessageId ??= msgChunk.MessageId;
                    currentMessageContent.Add(msgChunk.Delta ?? string.Empty);
                }
                else if (evt is TextMessageContentEvent msgContent)
                {
                    currentMessageContent.Add(msgContent.Delta ?? string.Empty);
                }
                else if (evt is TextMessageEndEvent msgEnd)
                {
                    if (currentMessageId == null || msgEnd.MessageId == currentMessageId)
                    {
                        FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);
                    }
                }
                // Track tool call events - handle both convenience (chunk) and full lifecycle patterns
                else if (evt is ToolCallChunkEvent toolChunk)
                {
                    // Convenience combined event: flush any buffered text, then record tool call
                    FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);

                    toolCalls.Add(new
                    {
                        id = toolChunk.ToolCallId,
                        name = toolChunk.ToolCallName,
                        args = toolChunk.Delta,
                        status = "called"
                    });
                }
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

            // Flush any remaining buffered message content (e.g. if stream ended without RunFinished)
            FlushCurrentMessage(messages, currentMessageContent, ref finalContent, ref currentMessageId);
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
