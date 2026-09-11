using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Umbraco.AI.Alibaba;

// Qwen's hybrid "thinking" models emit a `reasoning_content` field that Microsoft.Extensions.AI's
// ChatMessage doesn't round-trip across turns — the same problem DeepSeek has (see
// DeepSeekDisableThinkingPolicy in Umbraco.AI.DeepSeek). DashScope's OpenAI-compatible endpoint
// additionally rejects non-streaming calls to reasoning-capable models unless `enable_thinking`
// is passed explicitly.
//
// This policy injects `enable_thinking: false` into every chat completion request so behaviour
// stays predictable across turns. It skips models whose ID mentions "thinking" or "instruct" —
// Alibaba's docs describe "-thinking" models as thinking-only (they reject enable_thinking: false
// with a 400) and "-instruct" models as not supporting the toggle at all. This split is based on
// documentation, not a live-tested matrix per model family — verify during smoke testing and
// widen the skip list if another family turns out to reject the parameter.
internal sealed class AlibabaDisableThinkingPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        InjectThinkingDisabled(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        InjectThinkingDisabled(message);
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }

    private static void InjectThinkingDisabled(PipelineMessage message)
    {
        var path = message.Request?.Uri?.AbsolutePath;
        if (path is null || !path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (message.Request?.Content is not { } content)
        {
            return;
        }

        using var ms = new MemoryStream();
        content.WriteTo(ms, default);
        ms.Position = 0;

        if (JsonNode.Parse(ms) is not JsonObject body)
        {
            return;
        }

        var modelId = body["model"]?.GetValue<string>();
        if (modelId is not null &&
            (modelId.Contains("thinking", StringComparison.OrdinalIgnoreCase) ||
             modelId.Contains("instruct", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        body["enable_thinking"] = false;
        message.Request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
    }
}
