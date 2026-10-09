using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Observability;

/// <summary>
/// A tracking scope for a single AI operation. Created by <see cref="AIOperationTracker.BeginAsync"/>.
/// Completing or failing the scope measures the outcome once and hands it to each recording in
/// recorder order.
/// </summary>
/// <remarks>
/// A call begun while another call's scope is entered is nested in it (a guardrail judge, a search
/// embedding). The parent's duration leaves out the time its nested calls were running, so each moment of
/// AI work is counted once (#563).
/// </remarks>
internal sealed class AIOperationScope
{
    private static readonly AsyncLocal<AIOperationScope?> CurrentScope = new();

    private readonly AIOperationTracker _tracker;
    private readonly IReadOnlyList<IAIOperationRecording> _recordings;
    private readonly AIOperationScope? _parent;
    private readonly TimeProvider _timeProvider;
    private readonly long _startTimestamp;
    private readonly Lock _nestedLock = new();
    private int _runningNestedCalls;
    private TimeSpan _nestedStartedAt;
    private TimeSpan _nestedTime;
    private long? _durationMs;

    internal AIOperationScope(
        AIOperationTracker tracker,
        IReadOnlyList<IAIOperationRecording> recordings,
        AIOperationScope? parent,
        AIRuntimeContext? runtimeContext,
        TimeProvider timeProvider)
    {
        _tracker = tracker;
        _recordings = recordings;
        _parent = parent;
        RuntimeContext = runtimeContext;
        _timeProvider = timeProvider;
        _startTimestamp = timeProvider.GetTimestamp();
        parent?.NestedCallStarted();
    }

    /// <summary>
    /// The scope entered around the work currently running, if any. A call begun now is nested in it.
    /// </summary>
    internal static AIOperationScope? Current => CurrentScope.Value;

    /// <summary>
    /// The runtime context that was current when the call began. An AI call made inside this one while that
    /// context is still current gets its own copy (see <see cref="AIRuntimeContextCallScope"/>).
    /// </summary>
    internal AIRuntimeContext? RuntimeContext { get; }

    /// <summary>
    /// Whether the call has completed or failed. Work it started that is still running (e.g. on another
    /// task) can still see it as <see cref="Current"/>, but is no longer nested in it.
    /// </summary>
    internal bool HasEnded
    {
        get
        {
            lock (_nestedLock)
            {
                return _durationMs is not null;
            }
        }
    }

    public Task CompleteAsync(UsageDetails? usage, object? responseData)
        => _tracker.EndRecordingsAsync(
            _recordings,
            new AIOperationOutcome(AIOperationStatus.Succeeded, usage, End(), Exception: null, responseData));

    public Task FailAsync(Exception exception, UsageDetails? usage = null)
    {
        var status = exception is AIGuardrailBlockedException ? AIOperationStatus.Blocked : AIOperationStatus.Failed;
        return _tracker.EndRecordingsAsync(
            _recordings,
            new AIOperationOutcome(status, usage, End(), exception));
    }

    /// <summary>
    /// Makes this the <see cref="Current"/> scope, so calls begun inside it are nested in it, and opens each
    /// recording's ambient scope (today, the audit parent for nested calls). Dispose closes them all, in
    /// reverse order.
    /// </summary>
    /// <remarks>
    /// Enter it in the caller's own frame, directly around the work (for a stream, around each step of the
    /// inner enumerator): AsyncLocal changes made inside an async method or iterator do not survive its
    /// return or a yield.
    /// </remarks>
    public IDisposable EnterScope()
    {
        List<IDisposable> scopes = [new CurrentScopeRestorer(CurrentScope.Value)];
        CurrentScope.Value = this;

        foreach (var recording in _recordings)
        {
            if (recording.EnterScope() is { } scope)
            {
                scopes.Add(scope);
            }
        }

        return scopes is [var only] ? only : new CompositeScope(scopes);
    }

    /// <summary>
    /// Stops the clock (once) and returns the duration in milliseconds, leaving out time spent in nested calls.
    /// </summary>
    private long End()
    {
        lock (_nestedLock)
        {
            if (_durationMs is { } durationMs)
            {
                return durationMs;
            }

            var elapsed = _timeProvider.GetElapsedTime(_startTimestamp);

            // A nested call still running (e.g. an abandoned stream) counts as nested up to now.
            if (_runningNestedCalls > 0)
            {
                _nestedTime += elapsed - _nestedStartedAt;
            }

            _durationMs = (long)Math.Max(0, (elapsed - _nestedTime).TotalMilliseconds);
        }

        _parent?.NestedCallEnded();
        return _durationMs.Value;
    }

    private void NestedCallStarted()
    {
        lock (_nestedLock)
        {
            if (_durationMs is null && _runningNestedCalls++ == 0)
            {
                _nestedStartedAt = _timeProvider.GetElapsedTime(_startTimestamp);
            }
        }
    }

    // Overlapping nested calls are timed as one span, from the first starting to the last ending.
    private void NestedCallEnded()
    {
        lock (_nestedLock)
        {
            if (_durationMs is null && --_runningNestedCalls == 0)
            {
                _nestedTime += _timeProvider.GetElapsedTime(_startTimestamp) - _nestedStartedAt;
            }
        }
    }

    private sealed class CurrentScopeRestorer(AIOperationScope? previous) : IDisposable
    {
        public void Dispose() => CurrentScope.Value = previous;
    }

    private sealed class CompositeScope(List<IDisposable> scopes) : IDisposable
    {
        public void Dispose()
        {
            for (var i = scopes.Count - 1; i >= 0; i--)
            {
                scopes[i].Dispose();
            }
        }
    }
}
