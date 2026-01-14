using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Umbraco.Ai.Core.TaskQueue;

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
}