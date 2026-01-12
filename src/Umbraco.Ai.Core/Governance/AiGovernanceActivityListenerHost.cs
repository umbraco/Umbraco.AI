using Microsoft.Extensions.Hosting;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Hosted service that manages the lifecycle of the AI Governance ActivityListener.
/// </summary>
internal sealed class AiGovernanceActivityListenerHost : IHostedService
{
    private readonly AiGovernanceActivityListener _listener;

    public AiGovernanceActivityListenerHost(AiGovernanceActivityListener listener)
    {
        _listener = listener;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Listener is already registered in its constructor
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _listener.Dispose();
        return Task.CompletedTask;
    }
}
