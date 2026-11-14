using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Samples;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Abilities.Tests;

/// <summary>Side effects with retries run on Umbraco's background queue after apply, retried with Polly.</summary>
public class SideEffectRetryTests
{
    private readonly TestQueue _queue = new();
    private readonly RecordingEvents _events = new();
    private readonly SampleSite _site;
    private readonly ScriptedAgent _agent;

    public SideEffectRetryTests()
    {
        _site = SampleSite.Create(_queue, _events);
        _agent = new ScriptedAgent(_site);
    }

    [Fact]
    public async Task A_side_effect_with_retries_is_queued_after_apply_and_retried_until_it_succeeds()
    {
        await _agent.StartChangeSetAsync("Announce");
        await _agent.OkAsync("email_send", """{ "to": "team@example.com", "subject": "We're live" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        Assert.Empty(_site.Outbox.Sent); // queued, not run yet
        Assert.Single(_queue.Work);

        _site.Outbox.FailNextSends = 2;
        await _queue.RunAllAsync();

        var completed = Assert.Single(_events.Of<ChangeSetSideEffectCompletedNotification>());
        Assert.True(completed.Result.Success);
        Assert.Equal(3, completed.Attempts); // two failures, then sent
        Assert.Single(_site.Outbox.Sent);
    }

    [Fact]
    public async Task Retries_give_up_after_the_limit_and_the_failure_is_a_notification()
    {
        await _agent.StartChangeSetAsync("Announce");
        await _agent.OkAsync("email_send", """{ "to": "team@example.com", "subject": "We're live" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");

        _site.Outbox.ServerDown = true;
        await _queue.RunAllAsync();

        var completed = Assert.Single(_events.Of<ChangeSetSideEffectCompletedNotification>());
        Assert.Equal(AbilityOperationStatus.Failed, completed.Result.Status);
        Assert.Equal(4, completed.Attempts); // the first try and three retries
    }

    [Fact]
    public async Task Input_that_trying_again_cant_fix_is_not_retried()
    {
        await _agent.StartChangeSetAsync("Announce");
        await _agent.OkAsync("email_send", """{ "to": "not an address", "subject": "We're live" }""");
        await _agent.OkAsync("changeSet_apply", $$"""{ "changeSetId": "{{_agent.ChangeSetId}}" }""");
        await _queue.RunAllAsync();

        var completed = Assert.Single(_events.Of<ChangeSetSideEffectCompletedNotification>());
        Assert.Equal(AbilityOperationStatus.InvalidInput, completed.Result.Status);
        Assert.Equal(1, completed.Attempts);
    }

    /// <summary>Holds queued work until the test runs it, standing in for Umbraco's queued hosted service.</summary>
    private sealed class TestQueue : IBackgroundTaskQueue
    {
        public List<Func<CancellationToken, Task>> Work { get; } = [];

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem) => Work.Add(workItem);

        public Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
        {
            var next = Work.FirstOrDefault();
            if (next is not null)
            {
                Work.RemoveAt(0);
            }

            return Task.FromResult(next);
        }

        public async Task RunAllAsync()
        {
            while (await DequeueAsync(CancellationToken.None) is { } work)
            {
                await work(CancellationToken.None);
            }
        }
    }

    private sealed class RecordingEvents : IEventAggregator
    {
        private readonly List<INotification> _published = [];

        public IEnumerable<T> Of<T>() => _published.OfType<T>();

        public void Publish<TNotification>(TNotification notification)
            where TNotification : INotification
            => _published.Add(notification);

        public void Publish<TNotification, TNotificationHandler>(IEnumerable<TNotification> notifications)
            where TNotification : INotification
            where TNotificationHandler : INotificationHandler
            => _published.AddRange(notifications.Cast<INotification>());

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            _published.Add(notification);
            return Task.CompletedTask;
        }

        public Task PublishAsync<TNotification, TNotificationHandler>(IEnumerable<TNotification> notifications, CancellationToken cancellationToken = default)
            where TNotification : INotification
            where TNotificationHandler : INotificationHandler
        {
            _published.AddRange(notifications.Cast<INotification>());
            return Task.CompletedTask;
        }

        public bool PublishCancelable<TCancelableNotification>(TCancelableNotification notification)
            where TCancelableNotification : ICancelableNotification
        {
            _published.Add(notification);
            return false;
        }

        public Task<bool> PublishCancelableAsync<TCancelableNotification>(TCancelableNotification notification)
            where TCancelableNotification : ICancelableNotification
        {
            _published.Add(notification);
            return Task.FromResult(false);
        }
    }
}
