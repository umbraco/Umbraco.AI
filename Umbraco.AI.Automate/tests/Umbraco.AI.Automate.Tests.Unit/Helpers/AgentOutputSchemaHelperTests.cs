using System.Text.Json;
using Json.Schema;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Automate.Helpers;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Tests.Unit.Helpers;

public class AgentOutputSchemaHelperTests
{
    private readonly Mock<IAIAgentService> _agentServiceMock = new();
    private static readonly Guid TestAgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task GetOutputSchemaAsync_WithNoStructuredOutput_ReturnsRawResponseSchema()
    {
        // Arrange
        var agent = new AIAgent { Alias = "a", Name = "A", Config = new AIStandardAgentConfig() };
        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var schema = await AgentOutputSchemaHelper.GetOutputSchemaAsync(_agentServiceMock.Object, TestAgentId);

        // Assert
        var properties = PropertyNames(schema);
        properties.ShouldContain(RunAgentAction.RawResponseKey);
    }

    [Fact]
    public async Task GetOutputSchemaAsync_WithStructuredOutput_IncludesRawResponseAlongsideStructuredProperties()
    {
        // Arrange
        var outputSchema = JsonSerializer.Deserialize<JsonElement>(
            """{"type":"object","properties":{"summary":{"type":"string"},"score":{"type":"integer"}}}""");

        var agent = new AIAgent
        {
            Alias = "a",
            Name = "A",
            Config = new AIStandardAgentConfig { OutputSchema = outputSchema },
        };

        _agentServiceMock
            .Setup(s => s.GetAgentAsync(TestAgentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var schema = await AgentOutputSchemaHelper.GetOutputSchemaAsync(_agentServiceMock.Object, TestAgentId);

        // Assert
        var properties = PropertyNames(schema);
        properties.ShouldContain("summary");
        properties.ShouldContain("score");
        properties.ShouldContain(RunAgentAction.RawResponseKey);
    }

    private static IReadOnlyCollection<string> PropertyNames(JsonSchema schema)
    {
        var properties = schema.GetProperties();
        properties.ShouldNotBeNull();
        return properties!.Keys.ToList();
    }
}
