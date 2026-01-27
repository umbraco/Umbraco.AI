using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.DevUI.Models;

/// <summary>
/// Represents entity information for DevUI discovery.
/// </summary>
public record DevUIEntityInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("framework")] string Framework,
    [property: JsonPropertyName("tools")] List<string> Tools,
    [property: JsonPropertyName("metadata")] Dictionary<string, JsonElement> Metadata)
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("instructions")]
    public string? Instructions { get; init; }

    [JsonPropertyName("model_id")]
    public string? ModelId { get; init; }

    [JsonPropertyName("chat_client_type")]
    public string? ChatClientType { get; init; }

    [JsonPropertyName("executors")]
    public List<string>? Executors { get; init; }
}
