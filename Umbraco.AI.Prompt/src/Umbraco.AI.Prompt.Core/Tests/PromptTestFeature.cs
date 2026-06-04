using System.Diagnostics;
using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;
using AIConstants = Umbraco.AI.Core.Constants;
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

    /// <inheritdoc />
    public override string Description => "Tests prompt execution with mock entity context";

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTestFeature"/> class.
    /// </summary>
    [Obsolete("Use the constructor that also accepts an IAIEditableModelResolver. This constructor will be removed in a future version.")]
    public PromptTestFeature(
        IAIPromptService promptService,
        AITestContextResolver contextResolver,
        IAIEditableModelSchemaBuilder schemaBuilder)
        : base(contextResolver, schemaBuilder)
    {
        _promptService = promptService;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTestFeature"/> class.
    /// </summary>
    public PromptTestFeature(
        IAIPromptService promptService,
        AITestContextResolver contextResolver,
        IAIEditableModelSchemaBuilder schemaBuilder,
        IAIEditableModelResolver resolver)
        : base(contextResolver, schemaBuilder, resolver)
    {
        _promptService = promptService;
    }

    /// <inheritdoc />
    /// <remarks>
    /// For prompts with a result type of Single Option / Multiple Options, the underlying
    /// chat response is a structured-output JSON envelope (e.g. <c>{"value": "..."}</c>).
    /// The unwrapped text lives on <c>resultOptions[].displayValue</c>. Graders should
    /// evaluate the unwrapped text, not the JSON envelope, so prefer that when present.
    /// </remarks>
    public override string ExtractOutputValue(AITestTranscript transcript)
    {
        var envelope = transcript.FinalOutput.ValueKind == JsonValueKind.Object
            ? transcript.FinalOutput.Deserialize<FinalOutputEnvelope>(AIConstants.DefaultJsonSerializerOptions)
            : null;

        if (envelope?.ResultOptions is { Count: > 0 } options)
        {
            var displayValues = options
                .Select(o => o.DisplayValue)
                .Where(v => v is not null)
                .ToList();

            if (displayValues.Count > 0)
            {
                return displayValues.Count == 1
                    ? displayValues[0]!
                    : string.Join(Environment.NewLine, displayValues);
            }
        }

        return base.ExtractOutputValue(transcript);
    }

    /// <inheritdoc />
    public override async Task<AITestTranscript> ExecuteAsync(
        AITest test,
        int runNumber,
        Guid? profileIdOverride,
        IEnumerable<Guid>? contextIdsOverride,
        IEnumerable<Guid>? guardrailIdsOverride,
        CancellationToken cancellationToken)
    {
        // Resolve test feature config (resolves $Config app-settings references and validates)
        var config = ResolveTestFeatureConfig(test);
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize test feature config");
        }

        // Use target prompt ID directly (entity picker ensures valid ID)
        Guid promptId = test.TestTargetId;

        // Extract entity context from config
        var entityContext = ResolveEntityContext(config);

        // Mock entity → request.Context (raw AIRequestContextItem)
        var contextItems = ResolveEntityContextItems(config);

        // Build execution request
        var request = new AIPromptExecutionRequest
        {
            EntityId = Guid.Empty, // No real entity
            EntityType = entityContext?.EntityType ?? "document",
            ContentTypeAlias = entityContext?.EntitySubType ?? string.Empty,
            PropertyAlias = config.PropertyAlias,
            Context = contextItems.Count > 0 ? contextItems : null
        };

        // Execute prompt and capture timing
        var stopwatch = Stopwatch.StartNew();
        AIPromptExecutionResult result;

        try
        {
            var options = new AIPromptExecutionOptions
            {
                ValidateScope = false,
                ProfileIdOverride = profileIdOverride,
                ContextIdsOverride = contextIdsOverride?.ToList(),
                GuardrailIdsOverride = guardrailIdsOverride?.ToList()
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
                Messages = JsonSerializer.SerializeToElement(new[]
                {
                    new { role = "system", content = "Prompt execution failed" },
                    new { role = "error", content = ex.Message }
                }, AIConstants.DefaultJsonSerializerOptions),
                FinalOutput = JsonSerializer.SerializeToElement(
                    new FinalOutputEnvelope { Error = ex.Message, StackTrace = ex.StackTrace },
                    AIConstants.DefaultJsonSerializerOptions),
                Timing = JsonSerializer.SerializeToElement(new
                {
                    totalMs = stopwatch.ElapsedMilliseconds,
                    status = "error"
                }, AIConstants.DefaultJsonSerializerOptions)
            };
        }

        stopwatch.Stop();

        // Build messages array from actual chat messages sent to the AI model
        var messages = new List<object>();
        if (result.Messages != null)
        {
            foreach (var msg in result.Messages)
            {
                messages.Add(new { role = msg.Role.Value, content = msg.Text ?? string.Empty });
            }
        }

        // Append the assistant response
        messages.Add(new { role = "assistant", content = result.Content });

        // Build structured transcript
        return new AITestTranscript
        {
            RunId = Guid.NewGuid(), // Will be set by the runner
            Messages = JsonSerializer.SerializeToElement(messages, AIConstants.DefaultJsonSerializerOptions),
            ToolCalls = null, // Prompts don't typically use tool calls
            Reasoning = null, // No explicit reasoning in simple prompts
            Timing = JsonSerializer.SerializeToElement(new
            {
                totalMs = stopwatch.ElapsedMilliseconds,
                status = "success"
            }, AIConstants.DefaultJsonSerializerOptions),
            FinalOutput = JsonSerializer.SerializeToElement(
                new FinalOutputEnvelope
                {
                    Content = result.Content,
                    Usage = result.Usage is null ? null : new FinalOutputUsage
                    {
                        InputTokens = result.Usage.InputTokenCount,
                        OutputTokens = result.Usage.OutputTokenCount,
                        TotalTokens = result.Usage.TotalTokenCount
                    },
                    ResultOptions = result.ResultOptions
                },
                AIConstants.DefaultJsonSerializerOptions)
        };
    }

    /// <summary>
    /// Schema of the <see cref="AITestTranscript.FinalOutput"/> JSON for prompt tests.
    /// Defined as a single type so write-time and read-time stay in sync.
    /// </summary>
    internal sealed class FinalOutputEnvelope
    {
        public string? Content { get; init; }

        public FinalOutputUsage? Usage { get; init; }

        public IReadOnlyList<AIPromptExecutionResult.AIPromptResultOption>? ResultOptions { get; init; }

        public string? Error { get; init; }

        public string? StackTrace { get; init; }
    }

    internal sealed class FinalOutputUsage
    {
        public long? InputTokens { get; init; }

        public long? OutputTokens { get; init; }

        public long? TotalTokens { get; init; }
    }
}
