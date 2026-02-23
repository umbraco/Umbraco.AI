using System.Diagnostics;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Prompt.Core.Prompts;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Test feature for testing AI prompts.
/// Executes prompts with mock entity context and captures the response.
/// </summary>
[AITestFeature("prompt", "Prompt Test", Category = "Built-in")]
public class PromptTestFeature : AITestFeatureBase<PromptTestFeatureConfig>
{
    private readonly IAIPromptService _promptService;
    private readonly AITestContextResolver _contextResolver;

    /// <inheritdoc />
    public override string Description => "Tests prompt execution with mock entity context";

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTestFeature"/> class.
    /// </summary>
    public PromptTestFeature(
        IAIPromptService promptService,
        AITestContextResolver contextResolver,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(schemaBuilder)
    {
        _promptService = promptService;
        _contextResolver = contextResolver;
    }

    /// <inheritdoc />
    public override async Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        CancellationToken cancellationToken)
    {
        // Deserialize test feature config
        var config = test.GetTestFeatureConfig<PromptTestFeatureConfig>();
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize test feature config");
        }

        // Use target prompt ID directly (entity picker ensures valid ID)
        Guid promptId = test.TestTargetId;

        // Extract entity context from config
        var entityContext = config.EntityContext?.Deserialize<EntityContextConfig>();

        // Mock entity → request.Context (raw AIRequestContextItem)
        var contextItems = _contextResolver.ResolveContextItems(entityContext?.MockEntity);

        // Build execution request
        var request = new AIPromptExecutionRequest
        {
            EntityId = Guid.Empty, // No real entity
            EntityType = entityContext?.EntityType ?? "document",
            PropertyAlias = config.PropertyAlias,
            Culture = config.Culture,
            Segment = config.Segment,
            Context = contextItems.Count > 0 ? contextItems : null
        };

        // Context IDs → options.ContextIdsOverride (per-run override takes precedence)
        var effectiveContextIds = contextIdsOverride ?? config.ContextIds;

        // Execute prompt and capture timing
        var stopwatch = Stopwatch.StartNew();
        AIPromptExecutionResult result;

        try
        {
            var options = new AIPromptExecutionOptions
            {
                ValidateScope = false,
                ProfileIdOverride = profileIdOverride,
                ContextIdsOverride = effectiveContextIds?.ToList()
            };

            result = await _promptService.ExecutePromptAsync(promptId, request, options, cancellationToken);
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
            new { role = "system", content = $"Executing prompt for {entityContext?.EntityType ?? "document"}.{config.PropertyAlias}" },
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
