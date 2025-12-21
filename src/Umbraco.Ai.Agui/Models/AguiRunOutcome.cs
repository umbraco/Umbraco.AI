using System.Text.Json.Serialization;

namespace Umbraco.Ai.Agui.Models;

/// <summary>
/// Outcome of an agent run.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AguiRunOutcome>))]
public enum AguiRunOutcome
{
    /// <summary>
    /// Run completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Run was interrupted (human-in-the-loop).
    /// </summary>
    Interrupt
}
