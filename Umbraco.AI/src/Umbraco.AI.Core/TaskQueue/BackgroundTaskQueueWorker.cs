using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.TaskQueue;

internal sealed class BackgroundTaskQueueWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskQueueWorker> _logger;

    public BackgroundTaskQueueWorker(IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory, ILogger<BackgroundTaskQueueWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            BackgroundWorkItem item;
            try
            {
                item = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunItemOnCleanContextAsync(item, stoppingToken);
        }
    }

    // Dispatches the work item onto a fresh ThreadPool task with ExecutionContext
    // flow suppressed. Without this, AsyncLocal state (e.g. Umbraco's ambient
    // EF Core scope) from the producer thread can leak across the Channel when
    // the reader's awaiter completes synchronously on the writer, causing
    // "Scope being disposed is not the Ambient Scope" errors inside work items.
    private Task RunItemOnCleanContextAsync(BackgroundWorkItem item, CancellationToken stoppingToken)
    {
        AsyncFlowControl flow = ExecutionContext.SuppressFlow();
        try
        {
            return Task.Run(() => ExecuteItemAsync(item, stoppingToken), stoppingToken);
        }
        finally
        {
            flow.Undo();
        }
    }

    private async Task ExecuteItemAsync(BackgroundWorkItem item, CancellationToken stoppingToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            await item.RunAsync(scope.ServiceProvider, stoppingToken);

            _logger.LogInformation(
                "Background job '{JobName}' completed in {ElapsedMs}ms. CorrelationId={CorrelationId}",
                item.Name, sw.ElapsedMilliseconds, item.CorrelationId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Background job '{JobName}' failed after {ElapsedMs}ms. CorrelationId={CorrelationId}",
                item.Name, sw.ElapsedMilliseconds, item.CorrelationId);
            // swallow so caller + worker loop are not affected
        }
    }
}