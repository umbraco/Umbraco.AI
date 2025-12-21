using System.Text.Json;

namespace Umbraco.Ai.Agui.Streaming;

/// <summary>
/// Options for AG-UI streaming.
/// </summary>
public sealed class AguiStreamOptions
{
    /// <summary>
    /// Gets the default stream options.
    /// </summary>
    public static AguiStreamOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the JSON serializer options.
    /// Configured for AG-UI protocol compliance with polymorphic type handling.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true
    };

    /// <summary>
    /// Gets or sets the content type for SSE responses.
    /// </summary>
    public string ContentType { get; set; } = "text/event-stream";

    /// <summary>
    /// Gets or sets the cache control header value.
    /// </summary>
    public string CacheControl { get; set; } = "no-cache";
}
