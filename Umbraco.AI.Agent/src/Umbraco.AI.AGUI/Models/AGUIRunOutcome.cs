using System.Text.Json.Serialization;

namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Discriminated union describing the outcome of a finished agent run.
/// Per AG-UI spec, a <c>RUN_FINISHED</c> event carries an <c>outcome</c> field
/// whose <c>type</c> distinguishes <c>"success"</c> from <c>"interrupt"</c>.
/// Errors are signalled via <c>RUN_ERROR</c> instead and do not appear here.
/// </summary>
/// <remarks>
/// <para>
/// AG-UI spec: <see href="https://docs.ag-ui.com/concepts/interrupts"/>.
/// </para>
/// <para>
/// As of <c>@ag-ui/client@0.0.53</c> the published Zod schema for
/// <c>RunFinishedEventSchema</c> only exposes <c>result?: any</c> and has not
/// yet been updated to model <c>outcome</c>. The field still serialises through
/// Zod's <c>passthrough</c> on the wire, so consumers can read it today; once
/// the SDK schema catches up, the frontend can drop its local extension type
/// for <c>RunFinishedAGUIEvent</c>.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AGUIRunOutcomeSuccess), "success")]
[JsonDerivedType(typeof(AGUIRunOutcomeInterrupt), "interrupt")]
public abstract record AGUIRunOutcome;

/// <summary>
/// Run completed successfully without interrupts.
/// </summary>
public sealed record AGUIRunOutcomeSuccess : AGUIRunOutcome;

/// <summary>
/// Run paused for human input. The <c>Interrupts</c> array MUST be non-empty.
/// </summary>
/// <param name="Interrupts">One entry per open interrupt the client must address before resuming.</param>
public sealed record AGUIRunOutcomeInterrupt(
    [property: JsonPropertyName("interrupts")] IReadOnlyList<AGUIInterruptInfo> Interrupts) : AGUIRunOutcome;
