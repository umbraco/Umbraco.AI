using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentTypeSchemaToolTests
{
    private readonly Mock<IPublishedContentTypeCache> _publishedContentTypeCacheMock;
    private readonly IAITool _tool;

    public GetContentTypeSchemaToolTests()
    {
        _publishedContentTypeCacheMock = new Mock<IPublishedContentTypeCache>();
        _tool = new GetContentTypeSchemaTool(_publishedContentTypeCacheMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyAlias_ReturnsError(string? alias)
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(alias!);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("empty");
        schemaResult.Schema.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentAlias_ReturnsNotFound()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs("nonExistentType");

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(It.IsAny<PublishedItemType>(), "nonExistentType"))
            .Returns((IPublishedContentType?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("not found");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("schema");
    }
}
