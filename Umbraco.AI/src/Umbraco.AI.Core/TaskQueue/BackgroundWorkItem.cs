namespace Umbraco.AI.Core.TaskQueue;

internal sealed record BackgroundWorkItem(
    string Name,
    string? CorrelationId,
    Func<IServiceProvider, CancellationToken, Task> RunAsync);