using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Minimal <see cref="IAICapability"/> test double declaring <see cref="AICapability.Extraction"/>.
/// </summary>
/// <remarks>
/// Deliberately does not implement the real <c>IAIExtractionCapability</c>, like
/// <see cref="FakeDecisionCapability"/>: the experimental gating (hidden capability listing, empty
/// connections-by-capability, rejected profile creation) keys off <see cref="IAICapability.Kind"/> alone.
/// </remarks>
public class FakeExtractionCapability : IAICapability
{
    public AICapability Kind => AICapability.Extraction;

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        object? settings = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AIModelDescriptor>>([]);
}
