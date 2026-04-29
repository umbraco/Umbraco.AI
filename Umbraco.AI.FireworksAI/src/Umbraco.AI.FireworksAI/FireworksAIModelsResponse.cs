using System.Text.Json.Serialization;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// Response from the Fireworks AI native models list endpoint
/// (<c>GET /v1/accounts/{account_id}/models</c>).
/// </summary>
internal sealed class FireworksAIModelsResponse
{
    [JsonPropertyName("models")]
    public List<FireworksAIModelInfo> Models { get; set; } = [];

    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}

/// <summary>
/// A single model entry returned by the Fireworks native models endpoint.
/// Only the fields we need for capability classification are deserialised.
/// </summary>
internal sealed class FireworksAIModelInfo
{
    /// <summary>
    /// Fully qualified model name, e.g. <c>accounts/fireworks/models/llama-v3p3-70b-instruct</c>.
    /// This is the id that must be passed to the Chat Completions / Embeddings API.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Model category. Values include <c>HF_BASE_MODEL</c>, <c>HF_PEFT_ADDON</c>,
    /// <c>EMBEDDING_MODEL</c>, <c>FIRE_AGENT</c>. We use this to identify embedding models.
    /// </summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>
    /// Present iff the Chat Completions API is enabled for this model.
    /// Non-null means the model is a chat model.
    /// </summary>
    [JsonPropertyName("conversationConfig")]
    public FireworksAIConversationConfig? ConversationConfig { get; set; }

    /// <summary>
    /// Whether the model has a serverless deployment usable via the shared API.
    /// Dedicated-deployment-only models would return 404 for callers that don't own them.
    /// </summary>
    [JsonPropertyName("supportsServerless")]
    public bool SupportsServerless { get; set; }
}

/// <summary>
/// Placeholder — we only care that this property is present on the model,
/// not its contents.
/// </summary>
internal sealed class FireworksAIConversationConfig
{
}
