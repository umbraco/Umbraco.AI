using System.Diagnostics;
using System.Text.Json;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Tests;
using Umbraco.Ai.Prompt.Core.Prompts;

namespace Umbraco.Ai.Prompt.Core.Tests;

/// <summary>
/// Test feature for testing prompt execution.
/// Executes prompts with mock or real content context and captures transcripts.
/// </summary>
[AiTestFeature("prompt", "Prompt Test", Category = "Built-in")]
public class PromptTestFeature : IAiTestFeature
{
    private readonly IAiPromptService _promptService;
    private readonly IAiEditableModelSchemaBuilder _schemaBuilder;

    /// <inheritdoc />
    public string Id => "prompt";

    /// <inheritdoc />
    public string Name => "Prompt Test";

    /// <inheritdoc />
    public string Description => "Tests prompt execution with mock or real content context";

    /// <inheritdoc />
    public string Category => "Built-in";

    /// <inheritdoc />
    public Type? TestCaseType => typeof(PromptTestTestCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTestFeature"/> class.
    /// </summary>
    public PromptTestFeature(
        IAiPromptService promptService,
        IAiEditableModelSchemaBuilder schemaBuilder)
    {
        _promptService = promptService;
        _schemaBuilder = schemaBuilder;
    }

    /// <inheritdoc />
    public AiEditableModelSchema? GetTestCaseSchema()
    {
        return _schemaBuilder.BuildForType<PromptTestTestCase>(Id);
    }

    /// <inheritdoc />
    public async Task<AiTestTranscript> ExecuteAsync(
        AiTest test,
        int runNumber,
        CancellationToken cancellationToken = default)
    {
        // Deserialize test case from test.TestCase.TestCaseJson
        var testCase = JsonSerializer.Deserialize<PromptTestTestCase>(test.TestCase.TestCaseJson)
            ?? throw new InvalidOperationException("Failed to deserialize test case JSON");

        // Resolve target prompt from test
        var promptId = await ResolvePromptIdAsync(test.Target, cancellationToken);

        // Build execution request
        var request = new AiPromptExecutionRequest
        {
            EntityId = testCase.EntityId ?? Guid.Empty,
            EntityType = testCase.EntityType,
            PropertyAlias = testCase.PropertyAlias,
            Culture = testCase.Culture,
            Segment = testCase.Segment,
            Context = ParseContextItems(testCase.ContextItemsJson),
        };

        // Execute prompt and capture timing
        var stopwatch = Stopwatch.StartNew();
        AiPromptExecutionResult result;
        try
        {
            result = await _promptService.ExecutePromptAsync(promptId, request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Capture execution errors in transcript
            stopwatch.Stop();
            return new AiTestTranscript
            {
                MessagesJson = JsonSerializer.Serialize(new[]
                {
                    new { role = "system", content = "Prompt execution failed" },
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

        // Build structured transcript
        // Note: For prompts, we don't have access to the full chat history from the execution result,
        // so we reconstruct a simplified version showing the final response
        var messages = new List<object>
        {
            new { role = "system", content = $"Executing prompt: {test.Name}" },
            new { role = "assistant", content = result.Content },
        };

        // Return structured transcript
        return new AiTestTranscript
        {
            MessagesJson = JsonSerializer.Serialize(messages),
            ToolCallsJson = null, // Prompts don't use tools
            ReasoningJson = null, // Prompts don't expose reasoning
            TimingJson = JsonSerializer.Serialize(new
            {
                totalMs = stopwatch.ElapsedMilliseconds,
            }),
            FinalOutputJson = JsonSerializer.Serialize(new
            {
                content = result.Content,
                usage = result.Usage != null ? new
                {
                    inputTokens = result.Usage.InputTokenCount,
                    outputTokens = result.Usage.OutputTokenCount,
                    totalTokens = result.Usage.TotalTokenCount,
                } : null,
                propertyChanges = result.PropertyChanges,
            }),
        };
    }

    /// <summary>
    /// Resolves the prompt ID from the test target.
    /// </summary>
    private async Task<Guid> ResolvePromptIdAsync(AiTestTarget target, CancellationToken cancellationToken)
    {
        if (target.IsAlias)
        {
            var prompt = await _promptService.GetPromptByAliasAsync(target.TargetId, cancellationToken);
            if (prompt == null)
            {
                throw new InvalidOperationException($"Prompt with alias '{target.TargetId}' not found");
            }
            return prompt.Id;
        }

        if (Guid.TryParse(target.TargetId, out var promptId))
        {
            return promptId;
        }

        throw new InvalidOperationException($"Invalid prompt target ID: '{target.TargetId}'");
    }

    /// <summary>
    /// Parses context items from JSON string.
    /// </summary>
    private static IReadOnlyList<AiRequestContextItem>? ParseContextItems(string? contextItemsJson)
    {
        if (string.IsNullOrWhiteSpace(contextItemsJson))
        {
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<ContextItemDto>>(contextItemsJson);
            return items?.Select(x => new AiRequestContextItem
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
    /// DTO for deserializing context items from JSON.
    /// </summary>
    private sealed class ContextItemDto
    {
        public string? Description { get; set; }
        public required string Value { get; set; }
    }
}
