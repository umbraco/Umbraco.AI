using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit.Fakes;

/// <summary>
/// Minimal provider infrastructure for constructing a real provider in a test. Capabilities are created by
/// activation, which is enough for the provider's own capability wiring.
/// </summary>
internal sealed class FakeProviderInfrastructure : IAIProviderInfrastructure
{
    public IAICapabilityFactory CapabilityFactory { get; } = new ActivatorCapabilityFactory();

    public IAIEditableModelSchemaBuilder SchemaBuilder
        => throw new NotSupportedException("Schema building is not exercised by these tests.");

    private sealed class ActivatorCapabilityFactory : IAICapabilityFactory
    {
        public TCapability Create<TCapability>(IAIProvider provider)
            where TCapability : class, IAICapability
            => (TCapability)Activator.CreateInstance(typeof(TCapability), provider)!;
    }
}
