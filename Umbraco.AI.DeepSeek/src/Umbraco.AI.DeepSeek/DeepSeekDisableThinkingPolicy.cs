using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Umbraco.AI.DeepSeek;

// DeepSeek's v4 models default to "thinking" mode, which emits a `reasoning_content`
// field on the assistant message. Microsoft.Extensions.AI's ChatMessage doesn't
// preserve `reasoning_content` across turns, so any subsequent call (guardrails,
// evaluators, agent loops) sends the message back without it and DeepSeek rejects:
// "The 'reasoning_content' in the thinking mode must be passed back to the API."
//
// This policy disables thinking mode on every chat completion request by injecting
// `thinking: { type: "disabled" }` into the JSON body just before it leaves the SDK.
internal sealed class DeepSeekDisableThinkingPolicy : PipelinePolicy
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

        body["thinking"] = new JsonObject { ["type"] = "disabled" };
        message.Request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
    }
}
