using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetUmbracoContentToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly IAITool _tool;

    public GetUmbracoContentToolTests()
    {
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _tool = new GetUmbracoContentTool(_contentEditingServiceMock.Object);
    }

    private static Mock<IContent> CreateContentMock(Guid key, string name, string contentTypeAlias)
    {
        var contentTypeMock = new Mock<ISimpleContentType>();
        contentTypeMock.Setup(x => x.Alias).Returns(contentTypeAlias);

        var contentMock = new Mock<IContent>();
        contentMock.Setup(x => x.Key).Returns(key);
        contentMock.Setup(x => x.Name).Returns(name);
        contentMock.Setup(x => x.GetCultureName(It.IsAny<string?>())).Returns((string?)null);
        contentMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);
        contentMock.Setup(x => x.CreateDate).Returns(DateTime.UtcNow);
        contentMock.Setup(x => x.UpdateDate).Returns(DateTime.UtcNow);
        contentMock.Setup(x => x.Level).Returns(1);
        contentMock.Setup(x => x.Path).Returns("-1,1234");
        contentMock.Setup(x => x.Properties).Returns(new PropertyCollection());
        return contentMock;
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        // Arrange
        var args = new GetUmbracoContentArgs(Guid.Empty);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("empty");
        contentResult.Content.ShouldBeNull();
        _contentEditingServiceMock.Verify(x => x.GetAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentKey_ReturnsNotFound()
    {
        // Arrange
        var key = Guid.NewGuid();
        var args = new GetUmbracoContentArgs(key);
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IContent?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithDraftContent_ReturnsContent()
    {
        // Arrange — proves get_umbraco_content can fetch content that has never been published,
        // reading from the same business/draft layer the write tools use.
        var key = Guid.NewGuid();
        var args = new GetUmbracoContentArgs(key);
        var contentMock = CreateContentMock(key, "Draft Page", "page");
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var contentResult = result.ShouldBeOfType<GetUmbracoContentResult>();
        contentResult.Success.ShouldBeTrue();
        contentResult.Content.ShouldNotBeNull();
        contentResult.Content!.Key.ShouldBe(key);
        contentResult.Content.Name.ShouldBe("Draft Page");
        contentResult.Content.ContentType.ShouldBe("page");
        contentResult.Content.Url.ShouldBeNull();
        contentResult.Content.Parent.ShouldBeNull();
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("content");
        description.ShouldContain("property");
    }
}
