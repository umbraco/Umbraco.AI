using System.Diagnostics;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Prompt.Core.Prompts;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Test feature for testing AI prompts.
/// Executes prompts with mock or real content context and captures the response.
/// </summary>
[AITestFeature("prompt", "Prompt Test", Category = "Built-in")]
public class PromptTestFeature : AITestFeatureBase
{
    private readonly IAIPromptService _promptService;

    /// <inheritdoc />
    public override string Description => "Tests prompt execution with mock or real content context";

    /// <inheritdoc />
    public override Type? TestCaseType => typeof(PromptTestCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTestFeature"/> class.
    /// </summary>
    public PromptTestFeature(
        IAIPromptService promptService,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _promptService = promptService;
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
        var testCase = JsonSerializer.Deserialize<PromptTestCase>(test.TestCaseJson);
        if (testCase == null)
        {
            throw new InvalidOperationException("Failed to deserialize test case");
        }

        // Use target prompt ID directly (entity picker ensures valid ID)
        Guid promptId = test.TestTargetId;

        // Build execution request
        var request = new AIPromptExecutionRequest
        {
            EntityId = testCase.EntityId ?? Guid.Empty,
            EntityType = testCase.EntityType,
            PropertyAlias = testCase.PropertyAlias,
            Culture = testCase.Culture,
            Segment = testCase.Segment,
            Context = testCase.ContextItems
        };

        // Execute prompt and capture timing
        var stopwatch = Stopwatch.StartNew();
        AIPromptExecutionResult result;

        try
        {
            // TODO: Handle profileIdOverride and contextIdsOverride once prompt service supports it
            result = await _promptService.ExecutePromptAsync(promptId, request, cancellationToken);
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
                    new { role = "system", content = "Prompt execution failed" },
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

        // Build messages array (simplified for prompt execution)
        var messages = new[]
        {
            new { role = "system", content = $"Executing prompt for {testCase.EntityType}.{testCase.PropertyAlias}" },
            new { role = "assistant", content = result.Content }
        };

        // Build structured transcript
        return new AITestTranscript
        {
            RunId = Guid.NewGuid(), // Will be set by the runner
            MessagesJson = JsonSerializer.Serialize(messages),
            ToolCallsJson = null, // Prompts don't typically use tool calls
            ReasoningJson = null, // No explicit reasoning in simple prompts
            TimingJson = JsonSerializer.Serialize(new
            {
                totalMs = stopwatch.ElapsedMilliseconds,
                status = "success"
            }),
            FinalOutputJson = JsonSerializer.Serialize(new
            {
                content = result.Content,
                usage = result.Usage != null ? new
                {
                    inputTokens = result.Usage.InputTokenCount,
                    outputTokens = result.Usage.OutputTokenCount,
                    totalTokens = result.Usage.TotalTokenCount
                } : null,
                resultOptions = result.ResultOptions
            })
        };
    }
}
