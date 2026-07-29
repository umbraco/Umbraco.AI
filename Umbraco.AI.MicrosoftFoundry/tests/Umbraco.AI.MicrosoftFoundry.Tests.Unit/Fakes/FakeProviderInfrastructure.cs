using System.Reflection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.MicrosoftFoundry.Tests.Unit.Fakes;

/// <summary>
/// Minimal provider infrastructure for constructing a real provider in a test. Capabilities are created by
/// activation, which is enough for the provider's own capability wiring.
/// </summary>
internal sealed class FakeProviderInfrastructure : IAIProviderInfrastructure
{
    public IAICapabilityFactory CapabilityFactory { get; } = new ActivatorCapabilityFactory();

    public IAIEditableModelSchemaBuilder SchemaBuilder
        => throw new NotSupportedException("Schema building is not exercised by these tests.");

    /// <summary>
    /// Activates a capability by picking its greediest constructor and supplying the provider plus nulls.
    /// </summary>
    /// <remarks>
    /// The capabilities here take an optional logger alongside the provider, and logging is not what these
    /// tests are about, so every parameter after the provider is passed as null rather than mocked.
    /// </remarks>
    private sealed class ActivatorCapabilityFactory : IAICapabilityFactory
    {
        public TCapability Create<TCapability>(IAIProvider provider)
            where TCapability : class, IAICapability
        {
            var constructor = typeof(TCapability)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var arguments = constructor
                .GetParameters()
                .Select((p, i) => i == 0 ? provider : (object?)null)
                .ToArray();

            return (TCapability)constructor.Invoke(arguments);
        }
    }
}
