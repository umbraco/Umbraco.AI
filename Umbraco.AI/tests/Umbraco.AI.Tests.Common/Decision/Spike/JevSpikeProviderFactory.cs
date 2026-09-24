#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Moq;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// Builds a real, working <see cref="JevSpikeProvider"/> for specs that need to exercise the actual
/// spike provider rather than a minimal test double (e.g. <c>FakeDecisionCapability</c>). Only the
/// provider infrastructure is mocked - <see cref="JevSpikeDecisionCapability"/> is wired for real, so
/// no API key/network call happens until a spec explicitly calls <c>CreateClientAsync</c>.
/// </summary>
/// <remarks>
/// Single source of truth for this wiring - previously duplicated across
/// <c>JevSpikeProviderTests.BuildProvider()</c>, <c>AIConnectionServiceTests.CreateJevSpikeProvider()</c>,
/// and <c>AIProfileServiceTests.CreateJevSpikeProvider()</c>. A future change to
/// <see cref="JevSpikeProvider"/>'s constructor only needs updating here.
/// </remarks>
public static class JevSpikeProviderFactory
{
    public static JevSpikeProvider Create(IHttpClientFactory? httpClientFactory = null)
    {
        var infrastructureMock = new Mock<IAIProviderInfrastructure>();
        var capabilityFactoryMock = new Mock<IAICapabilityFactory>();
        infrastructureMock.Setup(x => x.CapabilityFactory).Returns(capabilityFactoryMock.Object);
        infrastructureMock.Setup(x => x.SchemaBuilder).Returns(Mock.Of<IAIEditableModelSchemaBuilder>());

        capabilityFactoryMock
            .Setup(x => x.Create<JevSpikeDecisionCapability>(It.IsAny<IAIProvider>()))
            .Returns<IAIProvider>(p => new JevSpikeDecisionCapability((JevSpikeProvider)p));

        return new JevSpikeProvider(infrastructureMock.Object, httpClientFactory ?? Mock.Of<IHttpClientFactory>());
    }
}
