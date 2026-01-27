using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.Ai.DevUI.Models;

/// <summary>
/// JSON serializer options for DevUI API responses.
/// </summary>
public static class DevUIJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
