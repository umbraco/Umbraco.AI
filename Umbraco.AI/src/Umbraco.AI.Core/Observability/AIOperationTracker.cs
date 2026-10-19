using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Observability;

/// <inheritdoc cref="IAIOperationTracker" />
internal sealed class AIOperationTracker : IAIOperationTracker
{
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IReadOnlyList<IAIOperationRecorder> _recorders;
    private readonly ILogger<AIOperationTracker> _logger;
    private readonly TimeProvider _timeProvider;

    public AIOperationTracker(
        IAIRuntimeContextAccessor contextAccessor,
        IEnumerable<IAIOperationRecorder> recorders,
        ILogger<AIOperationTracker> logger)
        : this(contextAccessor, recorders, logger, TimeProvider.System)
    {
    }

    internal AIOperationTracker(
        IAIRuntimeContextAccessor contextAccessor,
        IEnumerable<IAIOperationRecorder> recorders,
        ILogger<AIOperationTracker> logger,
        TimeProvider timeProvider)
    {
        _contextAccessor = contextAccessor;
        _recorders = recorders.ToList();
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<AITrackedOperationResult<TResult>> TrackAsync<TResult>(
        AIOperationDescriptor descriptor,
        Func<CancellationToken, Task<AITrackedOperationResult<TResult>>> operation,
        CancellationToken cancellationToken)
    {
        var scope = await BeginAsync(descriptor, cancellationToken);
        AITrackedOperationResult<TResult> result;
        try
        {
            using (scope.EnterScope())
            {
                result = await operation(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await scope.FailAsync(ex);
            throw;
        }

        if (result.Failure is { } failure)
        {
            await scope.FailAsync(failure, result.Usage);
        }
        else
        {
            await scope.CompleteAsync(result.Usage, result.ResponseData);
        }

        return result;
    }

    public async Task<AIOperationScope> BeginAsync(AIOperationDescriptor descriptor, CancellationToken cancellationToken)
    {
        var runtimeContext = _contextAccessor.Context;

        // Captured once, as the call starts, so recorders never re-read the live context, which can still
        // change before the call completes. Nested AI calls get their own context (AIRuntimeContextCallScope).
        var identity = runtimeContext is not null
            ? AIUsageContext.ExtractFromRuntimeContext(descriptor.Capability, runtimeContext)
            : null;

        // The scope entered around the work currently running, if this call is made inside another one that
        // hasn't ended yet.
        var parent = AIOperationScope.Current is { HasEnded: false } current ? current : null;

        var start = new AIOperationStart(descriptor, identity, runtimeContext.GetLogValues(), IsNested: parent is not null);
        var recordings = await BeginRecordingsAsync(start, cancellationToken);

        return new AIOperationScope(this, recordings, parent, runtimeContext, _timeProvider);
    }

    private async Task<IReadOnlyList<IAIOperationRecording>> BeginRecordingsAsync(
        AIOperationStart start,
        CancellationToken cancellationToken)
    {
        var recordings = new List<IAIOperationRecording>(_recorders.Count);
        foreach (var recorder in _recorders)
        {
            try
            {
                if (await recorder.BeginAsync(start, cancellationToken) is { } recording)
                {
                    recordings.Add(recording);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Recorder} failed to start recording {Capability}",
                    recorder.GetType().FullName, start.Descriptor.Capability);
            }
        }

        return recordings;
    }

    /// <summary>
    /// Hands a finished call's outcome to each recording, in recorder order. Never throws into the AI call.
    /// </summary>
    internal async Task EndRecordingsAsync(IReadOnlyList<IAIOperationRecording> recordings, AIOperationOutcome outcome)
    {
        foreach (var recording in recordings)
        {
            try
            {
                await recording.EndAsync(outcome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Recording} failed to record the end of an AI call", recording.GetType().FullName);
            }
        }
    }
}
