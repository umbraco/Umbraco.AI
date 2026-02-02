using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbraco.AI.Agui.Models;

/// <summary>
/// Outcome of an agent run.
/// Uses camelCase serialization to match AG-UI protocol conventions (e.g., "success", "interrupt", "error").
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AguiRunOutcome>))]
public enum AguiRunOutcome
{
    /// <summary>
    /// Run completed successfully.
    /// </summary>
    [JsonStringEnumMemberName("success")]
    Success,

    /// <summary>
    /// Run was interrupted (human-in-the-loop or tool execution).
    /// </summary>
    [JsonStringEnumMemberName("interrupt")]
    Interrupt,

    /// <summary>
    /// Run failed with an error.
    /// </summary>
    [JsonStringEnumMemberName("error")]
    Error
}
