using System.Text.Json;
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

    /// <summary>
    /// The model's context window in tokens, kept as the raw JSON value. Absent or non-positive when
    /// OpenRouter does not report one; the chat capability then declares no context window.
    /// </summary>
    /// <remarks>
    /// Deliberately not an <c>int?</c>: a typed read throws on a fractional or oversized value, and
    /// one odd entry would then fail the whole model list, including settings support for every
    /// other model. The chat capability reads it leniently instead.
    /// </remarks>
    [JsonPropertyName("context_length")]
    public JsonElement? ContextLength { get; set; }

    /// <summary>
    /// The model's per-token pricing in USD, or <c>null</c> when the entry carries none.
    /// </summary>
    [JsonPropertyName("pricing")]
    public OpenRouterModelPricing? Pricing { get; set; }
}

/// <summary>
/// Per-token USD pricing for an OpenRouter model. Values are decimal strings (e.g.
/// <c>0.000003</c>); free models report <c>"0"</c> and routers with no fixed price report
/// <c>"-1"</c>. They are kept as raw JSON values, not strings, so a price that arrives as a JSON
/// number cannot fail the whole model list; the consumer validates them.
/// </summary>
internal sealed class OpenRouterModelPricing
{
    /// <summary>
    /// Price of one input (prompt) token in USD.
    /// </summary>
    [JsonPropertyName("prompt")]
    public JsonElement? Prompt { get; set; }

    /// <summary>
    /// Price of one output (completion) token in USD.
    /// </summary>
    [JsonPropertyName("completion")]
    public JsonElement? Completion { get; set; }
}
