using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.Ai.Prompt.Core.Media;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Core.Templates.Processors;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.Ai.Prompt.Tests.Unit.Prompts;

public class AiPromptTemplateServiceTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IContentService> _mockContentService;
    private readonly Mock<IAiMediaImageResolver> _mockImageResolver;
    private readonly AiPromptTemplateService _service;

    public AiPromptTemplateServiceTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockContentService = new Mock<IContentService>();
        _mockImageResolver = new Mock<IAiMediaImageResolver>();

        var textProcessor = new TextTemplateVariableProcessor();
        var imageProcessor = new ImageTemplateVariableProcessor(
            _mockMediaService.Object,
            _mockContentService.Object,
            _mockImageResolver.Object,
            Mock.Of<ILogger<ImageTemplateVariableProcessor>>());

        _service = new AiPromptTemplateService(textProcessor, imageProcessor);
    }

    #region Basic Text Processing

    [Fact]
    public void ProcessTemplate_WithNoVariables_ReturnsSingleTextContent()
    {
        // Arrange
        var template = "Hello, world!";
        var context = new Dictionary<string, object?>();

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Hello, world!");
    }

    [Fact]
    public void ProcessTemplate_WithTextVariable_ReplacesVariable()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var context = new Dictionary<string, object?>
        {
            ["name"] = "John"
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Hello, John!");
    }

    [Fact]
    public void ProcessTemplate_WithMultipleTextVariables_ReplacesAll()
    {
        // Arrange
        var template = "{{greeting}}, {{name}}! Today is {{day}}.";
        var context = new Dictionary<string, object?>
        {
            ["greeting"] = "Hello",
            ["name"] = "John",
            ["day"] = "Monday"
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Hello, John! Today is Monday.");
    }

    [Fact]
    public void ProcessTemplate_WithMissingVariable_ReplacesWithEmpty()
    {
        // Arrange
        var template = "Hello, {{name}}!";
        var context = new Dictionary<string, object?>();

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Hello, !");
    }

    [Fact]
    public void ProcessTemplate_WithNestedPath_ResolvesCorrectly()
    {
        // Arrange
        var template = "User: {{user.name}}, Email: {{user.email}}";
        var context = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "Jane",
                ["email"] = "jane@example.com"
            }
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("User: Jane, Email: jane@example.com");
    }

    #endregion

    #region Image Processing

    [Fact]
    public void ProcessTemplate_WithImageVariable_ReturnsDataContentAndReferenceName()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "Here is the image: {{image:umbracoFile}}";
        var context = CreateMediaContext(entityId);

        SetupMediaWithImage(entityId, "umbracoFile", "/media/image.png", "image/png", "Test Image");

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(3);
        result[0].ShouldBeOfType<TextContent>();
        ((TextContent)result[0]).Text.ShouldBe("Here is the image: ");
        result[1].ShouldBeOfType<DataContent>();
        ((DataContent)result[1]).MediaType.ShouldBe("image/png");
        result[2].ShouldBeOfType<TextContent>();
        ((TextContent)result[2]).Text.ShouldBe(" [Image: Test Image]");
    }

    [Fact]
    public void ProcessTemplate_WithImageBetweenText_SplitsCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "Before {{image:photo}} After";
        var context = CreateMediaContext(entityId);

        SetupMediaWithImage(entityId, "photo", "/media/image.jpg", "image/jpeg", "Photo");

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        // Result: "Before ", DataContent, " [Image: Photo] After"
        result.Count.ShouldBe(3);
        ((TextContent)result[0]).Text.ShouldBe("Before ");
        result[1].ShouldBeOfType<DataContent>();
        ((TextContent)result[2]).Text.ShouldBe(" [Image: Photo] After");
    }

    [Fact]
    public void ProcessTemplate_WithMultipleImages_ReturnsAllImagesWithReferenceNames()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "{{image:photo1}}\n{{image:photo2}}";
        var context = CreateMediaContext(entityId);

        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue("photo1", null, null, false)).Returns("/media/image1.png");
        mockMedia.Setup(m => m.GetValue("photo2", null, null, false)).Returns("/media/image2.png");
        mockMedia.Setup(m => m.Name).Returns("Photo Album");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia.Object);

        _mockImageResolver
            .Setup(r => r.Resolve(It.IsAny<object?>()))
            .Returns(new AiImageContent { Data = new byte[] { 1, 2, 3 }, MediaType = "image/png" });

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        // Result: DataContent, " [Image: Photo Album]\n", DataContent, " [Image: Photo Album]"
        result.Count.ShouldBe(4);
        result[0].ShouldBeOfType<DataContent>();
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: Photo Album]\n");
        result[2].ShouldBeOfType<DataContent>();
        result[3].ShouldBeOfType<TextContent>();
        ((TextContent)result[3]).Text.ShouldBe(" [Image: Photo Album]");
    }

    [Fact]
    public void ProcessTemplate_WithFailedImageResolution_SkipsImage()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "Before {{image:missing}} After";
        var context = CreateMediaContext(entityId);

        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue("missing", null, null, false)).Returns("/media/missing.png");
        mockMedia.Setup(m => m.Name).Returns("Missing Image");
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia.Object);

        _mockImageResolver
            .Setup(r => r.Resolve(It.IsAny<object?>()))
            .Returns((AiImageContent?)null);

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("Before  After");
    }

    #endregion

    #region Mixed Content

    [Fact]
    public void ProcessTemplate_WithMixedContent_ProcessesCorrectly()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "Generate alt text for:\n{{image:umbracoFile}}\nMedia name: {{name}}";
        var context = CreateMediaContext(entityId);
        context["name"] = "My Image";

        SetupMediaWithImage(entityId, "umbracoFile", "/media/image.png", "image/png", "Test Media");

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        // Result: "Generate alt text for:\n", DataContent, " [Image: Test Media]\nMedia name: My Image"
        result.Count.ShouldBe(3);
        ((TextContent)result[0]).Text.ShouldBe("Generate alt text for:\n");
        result[1].ShouldBeOfType<DataContent>();
        ((TextContent)result[2]).Text.ShouldBe(" [Image: Test Media]\nMedia name: My Image");
    }

    [Fact]
    public void ProcessTemplate_AdjacentTextVariables_Consolidates()
    {
        // Arrange
        var template = "{{first}}{{second}}{{third}}";
        var context = new Dictionary<string, object?>
        {
            ["first"] = "A",
            ["second"] = "B",
            ["third"] = "C"
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("ABC");
    }

    #endregion

    #region Prefix Parsing

    [Fact]
    public void ProcessTemplate_WithUnknownPrefix_FallsBackToTextProcessor()
    {
        // Arrange
        // Unknown prefixes fall back to text processor which looks for the full expression in context
        var template = "{{unknown:path}}";
        var context = new Dictionary<string, object?>
        {
            ["unknown:path"] = "direct value"
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("direct value");
    }

    [Fact]
    public void ProcessTemplate_WithColonInPath_DoesNotConfuseAsPrefix()
    {
        // Test that object["dict"]["key"] pattern with colon doesn't break
        var template = "{{data.value}}";
        var context = new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object?>
            {
                ["value"] = "test"
            }
        };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("test");
    }

    [Fact]
    public void ProcessTemplate_ImagePrefix_CaseInsensitive()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "{{IMAGE:photo}}";
        var context = CreateMediaContext(entityId);

        SetupMediaWithImage(entityId, "photo", "/media/image.png", "image/png", "Test Photo");

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        // Result: DataContent, " [Image: Test Photo]"
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<DataContent>();
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: Test Photo]");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ProcessTemplate_EmptyTemplate_ReturnsEmptyList()
    {
        // Act
        var result = _service.ProcessTemplate("", new Dictionary<string, object?>());

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ProcessTemplate_OnlyWhitespace_ReturnsSingleTextContent()
    {
        // Act
        var result = _service.ProcessTemplate("   \n\t  ", new Dictionary<string, object?>());

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("   \n\t  ");
    }

    [Fact]
    public void ProcessTemplate_WithNullTemplate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _service.ProcessTemplate(null!, new Dictionary<string, object?>()));
    }

    [Fact]
    public void ProcessTemplate_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _service.ProcessTemplate("test", null!));
    }

    [Fact]
    public void ProcessTemplate_VariableAtStart_ProcessesCorrectly()
    {
        // Arrange
        var template = "{{name}} says hello";
        var context = new Dictionary<string, object?> { ["name"] = "Alice" };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("Alice says hello");
    }

    [Fact]
    public void ProcessTemplate_VariableAtEnd_ProcessesCorrectly()
    {
        // Arrange
        var template = "Hello, {{name}}";
        var context = new Dictionary<string, object?> { ["name"] = "Bob" };

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        result.Count.ShouldBe(1);
        ((TextContent)result[0]).Text.ShouldBe("Hello, Bob");
    }

    [Fact]
    public void ProcessTemplate_OnlyImageVariable_ReturnsDataContentAndReferenceName()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var template = "{{image:photo}}";
        var context = CreateMediaContext(entityId);

        SetupMediaWithImage(entityId, "photo", "/media/image.png", "image/png", "Solo Photo");

        // Act
        var result = _service.ProcessTemplate(template, context);

        // Assert
        // Result: DataContent, " [Image: Solo Photo]"
        result.Count.ShouldBe(2);
        result[0].ShouldBeOfType<DataContent>();
        result[1].ShouldBeOfType<TextContent>();
        ((TextContent)result[1]).Text.ShouldBe(" [Image: Solo Photo]");
    }

    #endregion

    #region Helper Methods

    private static Dictionary<string, object?> CreateMediaContext(Guid entityId)
    {
        return new Dictionary<string, object?>
        {
            ["entityId"] = entityId.ToString(),
            ["entityType"] = "media"
        };
    }

    private void SetupMediaWithImage(Guid entityId, string propertyAlias, string imagePath, string mediaType, string mediaName)
    {
        var mockMedia = new Mock<IMedia>();
        mockMedia.Setup(m => m.GetValue(propertyAlias, null, null, false)).Returns(imagePath);
        mockMedia.Setup(m => m.Name).Returns(mediaName);
        _mockMediaService.Setup(s => s.GetById(entityId)).Returns(mockMedia.Object);

        _mockImageResolver
            .Setup(r => r.Resolve(imagePath))
            .Returns(new AiImageContent { Data = new byte[] { 1, 2, 3 }, MediaType = mediaType });
    }

    #endregion
}
