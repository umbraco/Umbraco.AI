using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIAgentServiceIdTests
{
    private readonly Mock<IAIAgentRepository> _repositoryMock = new();
    private readonly AIAgentService _service;

    public AIAgentServiceIdTests()
    {
        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent a, Guid? _, CancellationToken _) => a);

        _service = new AIAgentService(
            _repositoryMock.Object,
            Mock.Of<IAIEntityVersionService>(),
            null!, // IAIAgentFactory
            null!, // IAGUIStreamingService
            null!, // IAGUIContextConverter
            null!, // IAGUIMessageConverter
            new AIToolCollection(() => []),
            null!, // IAIProfileService
            null!, // IAIGuardrailService
            null!, // IAIContextService
            Mock.Of<IEventAggregator>(),
            null); // IBackOfficeSecurityAccessor
    }

    [Fact]
    public async Task SaveAgentAsync_WithIdConstructor_KeepsTheId()
    {
        var knownId = Guid.NewGuid();
        var agent = new AIAgent(knownId) { Alias = "known-id", Name = "Known Id" };

        var result = await _service.SaveAgentAsync(agent);

        result.Id.ShouldBe(knownId);
    }

    [Fact]
    public async Task SaveAgentAsync_WithParameterlessConstructor_GeneratesAnId()
    {
        var agent = new AIAgent { Alias = "no-id", Name = "No Id" };

        var result = await _service.SaveAgentAsync(agent);

        result.Id.ShouldNotBe(Guid.Empty);
    }
}
