using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Tests.Unit.Observability;

/// <summary>
/// A tracked call made while another tracked call is running (a guardrail judge, a search embedding) is
/// nested in it. #563: its time is counted once, in its own outcome, not again in its parent's.
/// </summary>
public class AIOperationTrackerNestingTests
{
    private readonly ManualTimeProvider _clock = new();
    private readonly CapturingRecorder _recorder = new();

    [Fact]
    public async Task NestedCall_ParentDurationLeavesOutTheNestedCallsTime()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act
        await tracker.TrackAsync(Descriptor(AICapability.Chat), async _ =>
        {
            _clock.Advance(100);
            await tracker.TrackAsync(Descriptor(AICapability.Embedding), _ =>
            {
                _clock.Advance(300);
                return Task.FromResult(new AITrackedOperationResult<int> { Result = 0 });
            }, CancellationToken.None);
            _clock.Advance(50);
            return new AITrackedOperationResult<int> { Result = 0 };
        }, CancellationToken.None);

        // Assert
        _recorder.DurationOf(AICapability.Embedding).ShouldBe(300);
        _recorder.DurationOf(AICapability.Chat).ShouldBe(150);
    }

    [Fact]
    public async Task OverlappingNestedCalls_TheirSharedTimeIsLeftOutOnce()
    {
        // Arrange
        var tracker = CreateTracker();
        var parent = await tracker.BeginAsync(Descriptor(AICapability.Chat), CancellationToken.None);
        AIOperationScope first, second;

        // Act: two nested calls run side by side, overlapping for 10 of their 30 ms.
        using (parent.EnterScope())
        {
            _clock.Advance(10);
            first = await tracker.BeginAsync(Descriptor(AICapability.Embedding), CancellationToken.None);
            _clock.Advance(10);
            second = await tracker.BeginAsync(Descriptor(AICapability.SpeechToText), CancellationToken.None);
            _clock.Advance(10);
            await first.CompleteAsync(usage: null, responseData: null);
            _clock.Advance(10);
            await second.CompleteAsync(usage: null, responseData: null);
            _clock.Advance(10);
        }

        await parent.CompleteAsync(usage: null, responseData: null);

        // Assert: 50 ms in all, 30 ms of it inside nested calls.
        _recorder.DurationOf(AICapability.Chat).ShouldBe(20);
    }

    [Fact]
    public async Task CallsOneAfterAnother_AreNotNested()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act
        await tracker.TrackAsync(Descriptor(AICapability.Chat), _ =>
        {
            _clock.Advance(100);
            return Task.FromResult(new AITrackedOperationResult<int> { Result = 0 });
        }, CancellationToken.None);
        await tracker.TrackAsync(Descriptor(AICapability.Embedding), _ =>
        {
            _clock.Advance(40);
            return Task.FromResult(new AITrackedOperationResult<int> { Result = 0 });
        }, CancellationToken.None);

        // Assert
        _recorder.DurationOf(AICapability.Chat).ShouldBe(100);
        _recorder.DurationOf(AICapability.Embedding).ShouldBe(40);
        _recorder.Starts.ShouldAllBe(s => !s.IsNested);
    }

    [Fact]
    public async Task NestedCall_StartsAsNested()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act
        await tracker.TrackAsync(Descriptor(AICapability.Chat), async _ =>
        {
            await tracker.TrackAsync(
                Descriptor(AICapability.Embedding),
                _ => Task.FromResult(new AITrackedOperationResult<int> { Result = 0 }),
                CancellationToken.None);
            return new AITrackedOperationResult<int> { Result = 0 };
        }, CancellationToken.None);

        // Assert
        _recorder.Starts.Single(s => s.Descriptor.Capability == AICapability.Chat).IsNested.ShouldBeFalse();
        _recorder.Starts.Single(s => s.Descriptor.Capability == AICapability.Embedding).IsNested.ShouldBeTrue();
    }

    [Fact]
    public async Task CallMadeAfterItsParentEnded_IsNotNested()
    {
        // Arrange: work started inside a call (e.g. on another task) still sees that call's scope.
        var tracker = CreateTracker();
        var parent = await tracker.BeginAsync(Descriptor(AICapability.Chat), CancellationToken.None);

        // Act
        using (parent.EnterScope())
        {
            await parent.CompleteAsync(usage: null, responseData: null);
            await tracker.TrackAsync(
                Descriptor(AICapability.Embedding),
                _ => Task.FromResult(new AITrackedOperationResult<int> { Result = 0 }),
                CancellationToken.None);
        }

        // Assert
        _recorder.Starts.Single(s => s.Descriptor.Capability == AICapability.Embedding).IsNested.ShouldBeFalse();
    }

    private AIOperationTracker CreateTracker() => new(
        Mock.Of<IAIRuntimeContextAccessor>(),
        [_recorder],
        NullLogger<AIOperationTracker>.Instance,
        _clock);

    private static AIOperationDescriptor Descriptor(AICapability capability) => new() { Capability = capability };

    private sealed class CapturingRecorder : IAIOperationRecorder
    {
        private readonly List<(AICapability Capability, AIOperationOutcome Outcome)> _outcomes = [];

        public List<AIOperationStart> Starts { get; } = [];

        public long DurationOf(AICapability capability) => _outcomes.Single(o => o.Capability == capability).Outcome.DurationMs;

        public ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken)
        {
            Starts.Add(start);
            return ValueTask.FromResult<IAIOperationRecording?>(new Recording(start.Descriptor.Capability, _outcomes));
        }

        private sealed class Recording(
            AICapability capability,
            List<(AICapability, AIOperationOutcome)> outcomes) : IAIOperationRecording
        {
            public ValueTask EndAsync(AIOperationOutcome outcome)
            {
                outcomes.Add((capability, outcome));
                return ValueTask.CompletedTask;
            }
        }
    }

    /// <summary>A clock that only moves when told to, in whole milliseconds.</summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(int milliseconds) => _timestamp += milliseconds * TimeSpan.TicksPerMillisecond;
    }
}
