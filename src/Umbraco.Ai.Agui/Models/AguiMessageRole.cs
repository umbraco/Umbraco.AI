using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Role of a message in the AG-UI protocol.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AguiMessageRole>))]
public enum AguiMessageRole
{
    /// <summary>
    /// End user input.
    /// </summary>
    User,

    /// <summary>
    /// AI assistant responses.
    /// </summary>
    Assistant,

    /// <summary>
    /// System instructions/context.
    /// </summary>
    System,

    /// <summary>
    /// Tool execution results.
    /// </summary>
    Tool,

    /// <summary>
    /// Internal debugging messages.
    /// </summary>
    Developer,

    /// <summary>
    /// Frontend-only UI updates.
    /// </summary>
    Activity
}
