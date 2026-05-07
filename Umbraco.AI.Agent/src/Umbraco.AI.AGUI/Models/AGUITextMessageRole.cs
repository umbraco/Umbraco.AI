using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Restricted role enum for AG-UI text-message events. Per spec, <c>TEXT_MESSAGE_START</c>
/// and <c>TEXT_MESSAGE_CHUNK</c> only allow <c>developer</c>, <c>system</c>, <c>assistant</c>,
/// or <c>user</c> — not <c>tool</c>, <c>activity</c>, or <c>reasoning</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AGUITextMessageRole>))]
public enum AGUITextMessageRole
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
    /// Internal debugging messages.
    /// </summary>
    [JsonStringEnumMemberName("developer")]
    Developer
}
