using Moq;
using Shouldly;
using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Core.Tests;
using Xunit;

namespace Umbraco.Ai.Prompt.Tests.Unit.Tests;

/// <summary>
/// Unit tests for PromptTestFeature.
/// These tests verify metadata and schema generation, not full execution integration.
/// </summary>
public class PromptTestFeatureTests
{
    private readonly Mock<IAiPromptService> _mockPromptService;
    private readonly Mock<IAiEditableModelSchemaBuilder> _mockSchemaBuilder;
    private readonly PromptTestFeature _feature;

    public PromptTestFeatureTests()
    {
        _mockPromptService = new Mock<IAiPromptService>();
        _mockSchemaBuilder = new Mock<IAiEditableModelSchemaBuilder>();
        _feature = new PromptTestFeature(_mockPromptService.Object, _mockSchemaBuilder.Object);
    }

    #region Metadata Tests

    [Fact]
    public void Id_ReturnsPrompt()
    {
        // Act & Assert
        _feature.Id.ShouldBe("prompt");
    }

    [Fact]
    public void Name_ReturnsPromptTest()
    {
        // Act & Assert
        _feature.Name.ShouldBe("Prompt Test");
    }

    [Fact]
    public void Description_ReturnsExpectedDescription()
    {
        // Act & Assert
        _feature.Description.ShouldBe("Tests prompt execution with mock or real content context");
    }

    [Fact]
    public void Category_ReturnsBuiltIn()
    {
        // Act & Assert
        _feature.Category.ShouldBe("Built-in");
    }

    [Fact]
    public void TestCaseType_ReturnsPromptTestTestCase()
    {
        // Act & Assert
        _feature.TestCaseType.ShouldBe(typeof(PromptTestTestCase));
    }

    #endregion

    #region Schema Tests

    [Fact]
    public void GetTestCaseSchema_CallsSchemaBuilder()
    {
        // Arrange
        var expectedSchema = new AiEditableModelSchema(typeof(PromptTestTestCase), Array.Empty<AiEditableModelField>());
        _mockSchemaBuilder
            .Setup(x => x.BuildForType<PromptTestTestCase>("prompt"))
            .Returns(expectedSchema);

        // Act
        var result = _feature.GetTestCaseSchema();

        // Assert
        result.ShouldBe(expectedSchema);
        _mockSchemaBuilder.Verify(x => x.BuildForType<PromptTestTestCase>("prompt"), Times.Once);
    }

    [Fact]
    public void GetTestCaseSchema_PassesCorrectFeatureIdToBuilder()
    {
        // Arrange
        var mockSchema = new AiEditableModelSchema(typeof(PromptTestTestCase), Array.Empty<AiEditableModelField>());
        _mockSchemaBuilder
            .Setup(x => x.BuildForType<PromptTestTestCase>(It.IsAny<string>()))
            .Returns(mockSchema);

        // Act
        _feature.GetTestCaseSchema();

        // Assert
        _mockSchemaBuilder.Verify(x => x.BuildForType<PromptTestTestCase>("prompt"), Times.Once);
    }

    #endregion
}
