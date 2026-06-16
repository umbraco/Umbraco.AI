using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Role on AG-UI tool-call lifecycle events. Per spec, <c>TOOL_CALL_RESULT</c>
/// allows only the literal <c>"tool"</c>. Modelled as a single-value enum so the
/// type system enforces the constraint.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AGUIToolCallRole>))]
public enum AGUIToolCallRole
{
    /// <summary>
    /// Tool execution result.
    /// </summary>
    [JsonStringEnumMemberName("tool")]
    Tool
}
