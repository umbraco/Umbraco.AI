using System.Diagnostics;
using Umbraco.AI.Core.AuditLog;

namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// The <c>umbraco.ai.*</c> tags of the tracked call whose work is running, and the one place that puts them on
/// its gen_ai span (#562).
/// </summary>
/// <remarks>
/// <see cref="AITraceOperationRecorder"/> builds the tags when a call starts and makes them current around the
/// call's work. The span only exists later, inside the OpenTelemetry middleware, which calls
/// <see cref="Apply"/> once it has started it.
/// </remarks>
internal static class AITraceTags
{
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> CurrentTags = new();

    /// <summary>
    /// Makes <paramref name="tags"/> the running call's tags until the returned scope is disposed. A call with
    /// no tags enters an empty set, so it never picks up the tags of a call it is nested in.
    /// </summary>
    public static IDisposable Enter(IReadOnlyDictionary<string, string> tags)
    {
        var scope = new Scope(CurrentTags.Value);
        CurrentTags.Value = tags;
        return scope;
    }

    /// <summary>
    /// Tags <paramref name="activity"/> with the running call's tags when it is a span from
    /// <see cref="AITelemetry.SourceName"/>, along with the call's audit entry ID, if it has one.
    /// </summary>
    public static void Apply(Activity? activity)
    {
        if (activity?.Source.Name != AITelemetry.SourceName || CurrentTags.Value is not { Count: > 0 } tags)
        {
            return;
        }

        foreach (var (tag, value) in tags)
        {
            activity.SetTag(tag, value);
        }

        // The audit recorder makes the call's own entry current around the same work.
        if (AIAuditScope.Current is { } auditScope)
        {
            activity.SetTag(AITelemetry.Tags.AuditId, auditScope.AuditLogId.ToString());
        }
    }

    private sealed class Scope(IReadOnlyDictionary<string, string>? previous) : IDisposable
    {
        public void Dispose() => CurrentTags.Value = previous;
    }
}
