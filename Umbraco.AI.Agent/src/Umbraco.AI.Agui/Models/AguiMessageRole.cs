using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Role of a message in the AG-UI protocol.
/// Uses lowercase serialization to match AG-UI protocol conventions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AguiMessageRole>))]
public enum AguiMessageRole
{
    /// <summary>
    /// End user input.
    /// </summary>
    [JsonStringEnumMemberName("user")]
    User,

    /// <summary>
    /// AI assistant responses.
    /// </summary>
    [JsonStringEnumMemberName("assistant")]
    Assistant,

    /// <summary>
    /// System instructions/context.
    /// </summary>
    [JsonStringEnumMemberName("system")]
    System,

    /// <summary>
    /// Tool execution results.
    /// </summary>
    [JsonStringEnumMemberName("tool")]
    Tool,

    /// <summary>
    /// Internal debugging messages.
    /// </summary>
    [JsonStringEnumMemberName("developer")]
    Developer,

    /// <summary>
    /// Frontend-only UI updates.
    /// </summary>
    [JsonStringEnumMemberName("activity")]
    Activity
}
