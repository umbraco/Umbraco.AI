using System.Text.Json.Serialization;

namespace Umbraco.AI.OpenRouter;

/// <summary>
/// Response from the OpenRouter models list endpoint (<c>GET /models</c>).
/// </summary>
internal sealed class OpenRouterModelsResponse
{
    [JsonPropertyName("data")]
    public List<OpenRouterModelInfo> Data { get; set; } = [];
}

/// <summary>
/// A single model entry returned by the OpenRouter models endpoint. Only the fields the chat
/// capability needs are deserialised.
/// </summary>
internal sealed class OpenRouterModelInfo
{
    /// <summary>
    /// Vendor-prefixed model slug, e.g. <c>openai/gpt-4o</c> or <c>anthropic/claude-3.5-sonnet</c>.
    /// This is the id that must be passed to the Chat Completions API.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// OpenRouter's own human-readable label for the model, e.g. <c>Anthropic: Claude v2.0</c>.
    /// Preferred over a generated display name when present.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The request parameters OpenRouter reports this model as accepting, e.g.
    /// <c>temperature</c>, <c>tools</c>, <c>response_format</c>, <c>reasoning_effort</c>. Drives
    /// <see cref="OpenRouterChatCapability.GetSettingsSupport"/> instead of a hardcoded model list,
    /// since OpenRouter fronts hundreds of models across dozens of vendors that each restrict
    /// differently.
    /// </summary>
    [JsonPropertyName("supported_parameters")]
    public List<string>? SupportedParameters { get; set; }
}
