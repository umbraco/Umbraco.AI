using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Umbraco.AI.Core.Hosting;

/// <summary>
/// Base class for Umbraco AI recurring hosted services that mirrors v18's
/// <c>RecurringBackgroundJobHostedService&lt;TJob&gt;</c> by suppressing execution-context flow around
/// <see cref="BackgroundService.StartAsync(CancellationToken)" />.
/// </summary>
/// <remarks>
/// <para>
/// Umbraco v18 keeps its ambient EF Core scope and scope-context stacks in <see cref="AsyncLocal{T}" />.
/// Hosted-service fire-and-forget loops that capture the host's execution context end up sharing those
/// stacks across every service, so a scope created inside one loop can be popped from the stack by an
/// unrelated loop. The resulting "No AmbientContext was found" / "not the ambient scope" failures
/// surface at <c>Scope.Dispose</c>, exactly the failure mode CMS PR #22331 documented.
/// </para>
/// <para>
/// CMS solves this by wrapping <c>base.StartAsync</c> in <see cref="ExecutionContext.SuppressFlow" />
/// inside its own <c>RecurringBackgroundJobHostedService&lt;TJob&gt;</c>. We mirror that here so any
/// Umbraco AI job that inherits this base gets the same protection without each implementation
/// having to remember to do it. Once we migrate the jobs to <c>IRecurringBackgroundJob</c> + the
/// CMS wrapper they can drop this base.
/// </para>
/// </remarks>
internal abstract class UmbracoAIRecurringHostedServiceBase : RecurringHostedServiceBase
{
    /// <inheritdoc />
    protected UmbracoAIRecurringHostedServiceBase(ILogger? logger, TimeSpan period, TimeSpan delay)
        : base(logger, period, delay)
    {
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Task startTask;
        using (ExecutionContext.IsFlowSuppressed() ? null : (IDisposable?)ExecutionContext.SuppressFlow())
        {
            startTask = base.StartAsync(cancellationToken);
        }

        await startTask;
    }
}
