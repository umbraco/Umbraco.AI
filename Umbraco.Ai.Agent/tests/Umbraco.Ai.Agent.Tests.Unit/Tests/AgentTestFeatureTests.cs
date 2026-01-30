using Moq;
using Shouldly;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Tests;
using Umbraco.Ai.Core.EditableModels;
using Xunit;

namespace Umbraco.Ai.Agent.Tests.Unit.Tests;

/// <summary>
/// Unit tests for AgentTestFeature.
/// These tests verify metadata and schema generation, not full execution integration.
/// </summary>

public class AgentTestFeatureTests
{
    private readonly Mock<IAiAgentService> _mockAgentService;
    private readonly Mock<IAiEditableModelSchemaBuilder> _mockSchemaBuilder;
    private readonly AgentTestFeature _feature;

    public AgentTestFeatureTests()
    {
        _mockAgentService = new Mock<IAiAgentService>();
        _mockSchemaBuilder = new Mock<IAiEditableModelSchemaBuilder>();
        _feature = new AgentTestFeature(_mockAgentService.Object, _mockSchemaBuilder.Object);
    }

    #region Metadata Tests

    [Fact]
    public void Id_ReturnsAgent()
    {
        // Act & Assert
        _feature.Id.ShouldBe("agent");
    }

    [Fact]
    public void Name_ReturnsAgentTest()
    {
        // Act & Assert
        _feature.Name.ShouldBe("Agent Test");
    }

    [Fact]
    public void Description_ReturnsDescription()
    {
        // Act & Assert
        _feature.Description.ShouldBe("Tests agent execution with messages and tools");
    }

    [Fact]
    public void Category_ReturnsBuiltIn()
    {
        // Act & Assert
        _feature.Category.ShouldBe("Built-in");
    }

    [Fact]
    public void TestCaseType_ReturnsAgentTestTestCase()
    {
        // Act & Assert
        _feature.TestCaseType.ShouldBe(typeof(AgentTestTestCase));
    }

    #endregion

    #region Schema Tests

    [Fact]
    public void GetTestCaseSchema_CallsSchemaBuilder()
    {
        // Arrange
        var expectedSchema = new AiEditableModelSchema(typeof(AgentTestTestCase), Array.Empty<AiEditableModelField>());
        _mockSchemaBuilder
            .Setup(x => x.BuildForType<AgentTestTestCase>("agent"))
            .Returns(expectedSchema);

        // Act
        var result = _feature.GetTestCaseSchema();

        // Assert
        result.ShouldBe(expectedSchema);
        _mockSchemaBuilder.Verify(x => x.BuildForType<AgentTestTestCase>("agent"), Times.Once);
    }

    #endregion
}
