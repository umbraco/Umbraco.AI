using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.DeepSeek;

/// <summary>
/// AI chat capability for the DeepSeek provider.
/// </summary>
public class DeepSeekChatCapability(DeepSeekProvider provider) : AIChatCapabilityBase<DeepSeekProviderSettings>(provider)
{
    private const string DefaultChatModel = "deepseek-chat";

    private new DeepSeekProvider Provider => (DeepSeekProvider)base.Provider;

    /// <summary>
    /// Patterns that match DeepSeek chat models. Broad on purpose so new model
    /// families (e.g. deepseek-v5-*) are picked up without code changes.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^deepseek-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        DeepSeekProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                DeepSeekModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(DeepSeekProviderSettings settings, string? modelId)
    {
        var inner = DeepSeekProvider.CreateDeepSeekClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();
        return new DeepSeekChatClient(inner);
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));

    // DeepSeek's chat API has two response_format quirks vs OpenAI:
    //   1. It only supports `{type: "json_object"}` — the richer `{type: "json_schema"}`
    //      and `{type: "text"}` variants return HTTP 400 "This response_format type is
    //      unavailable now".
    //   2. JSON mode requires the prompt to contain the word "json" (per DeepSeek docs);
    //      otherwise the model may return empty content.
    //
    // So when callers ask for JSON output we downgrade to bare json_object and inject
    // the schema (if any) as a system-prompt instruction. Text-mode is stripped — it's
    // DeepSeek's default anyway.
    private sealed class DeepSeekChatClient(IChatClient inner) : DelegatingChatClient(inner)
    {
        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var (m, o) = Rewrite(messages, options);
            return base.GetResponseAsync(m, o, cancellationToken);
        }

        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var (m, o) = Rewrite(messages, options);
            return base.GetStreamingResponseAsync(m, o, cancellationToken);
        }

        private static (IEnumerable<ChatMessage> messages, ChatOptions? options) Rewrite(
            IEnumerable<ChatMessage> messages, ChatOptions? options)
        {
            if (options?.ResponseFormat is null)
            {
                return (messages, options);
            }

            var clone = options.Clone();

            if (options.ResponseFormat is not ChatResponseFormatJson json)
            {
                clone.ResponseFormat = null;
                return (messages, clone);
            }

            clone.ResponseFormat = ChatResponseFormat.Json;

            var instruction = json.Schema is { } schema
                ? "You MUST respond with ONLY valid JSON matching this schema. No markdown fences, no commentary, no explanation — just the raw JSON object:\n\n" + schema.GetRawText()
                : "You MUST respond with ONLY valid JSON. No markdown fences, no commentary, no explanation — just the raw JSON object.";

            var instructionMessage = new ChatMessage(ChatRole.System, instruction);
            return (messages.Prepend(instructionMessage), clone);
        }
    }
}
