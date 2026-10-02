using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Minimal <see cref="IAICapability"/> test double declaring <see cref="AICapability.Decision"/>.
/// </summary>
/// <remarks>
/// Deliberately does not implement the real <c>IAIDecisionCapability</c> — DC-1's experimental
/// gating (hidden capability listing, empty connections-by-capability, rejected profile
/// creation) keys off <see cref="IAICapability.Kind"/> alone, so this proves that generic wiring
/// without waiting on the full client/factory/service plumbing to exist.
/// </remarks>
public class FakeDecisionCapability : IAICapability
{
    public AICapability Kind => AICapability.Decision;

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        object? settings = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);
}
