using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIContextServiceConnectorTests
{
    private readonly Mock<IAIContextService> _contextServiceMock = new();
    private readonly UmbracoAIContextServiceConnector _connector;
    private AIContext? _saved;

    public UmbracoAIContextServiceConnectorTests()
    {
        var settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        _contextServiceMock
            .Setup(x => x.SaveContextAsync(It.IsAny<AIContext>(), It.IsAny<CancellationToken>()))
            .Returns<AIContext, CancellationToken>((c, _) =>
            {
                _saved = c;
                return Task.FromResult(c);
            });
        _connector = new UmbracoAIContextServiceConnector(
            _contextServiceMock.Object,
            settingsAccessorMock.Object,
            new Umbraco.AI.Core.Contexts.ResourceTypes.AIContextResourceTypeCollection(() => []));
    }

    [Fact]
    public async Task RoundTrip_KeepsResourceIdsAndProperties()
    {
        var contextId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var source = new AIContext(contextId)
        {
            Alias = "brand",
            Name = "Brand",
            Resources =
            [
                new AIContextResource(firstId)
                {
                    ResourceTypeId = "brand-voice",
                    Name = "Voice",
                    Description = "Tone",
                    SortOrder = 1,
                    InjectionMode = AIContextResourceInjectionMode.OnDemand,
                    Settings = new Dictionary<string, object> { ["tone"] = "friendly" },
                },
                new AIContextResource(secondId) { ResourceTypeId = "text", Name = "Text", SortOrder = 2 },
            ],
        };

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, contextId);
        var artifact = await _connector.GetArtifactAsync(udi, source);
        await ImportAsync(artifact!);

        _saved!.Resources.Select(r => r.Id).ShouldBe([firstId, secondId]);
        var first = _saved.Resources[0];
        first.ResourceTypeId.ShouldBe("brand-voice");
        first.Name.ShouldBe("Voice");
        first.Description.ShouldBe("Tone");
        first.SortOrder.ShouldBe(1);
        first.InjectionMode.ShouldBe(AIContextResourceInjectionMode.OnDemand);
        ((JsonElement)first.Settings!).GetProperty("tone").GetString().ShouldBe("friendly");
    }

    [Fact]
    public async Task Import_LegacyArtifactWithoutResourceIds_LeavesIdsEmpty()
    {
        var artifact = new AIContextArtifact(new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, Guid.NewGuid()))
        {
            Alias = "brand",
            Name = "Brand",
            Resources = JsonDocument.Parse("""[{"ResourceTypeId":"text","Name":"Text","SortOrder":0,"InjectionMode":0}]""").RootElement,
        };

        await ImportAsync(artifact);

        _saved!.Resources.Single().Id.ShouldBe(Guid.Empty);
    }

    private Task ImportAsync(AIContextArtifact artifact)
    {
        var state = new ArtifactDeployState<AIContextArtifact, AIContext>(artifact, null, _connector, 2);
        return _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);
    }
}
