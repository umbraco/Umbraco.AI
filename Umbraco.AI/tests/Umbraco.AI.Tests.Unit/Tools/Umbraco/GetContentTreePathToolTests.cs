using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentTreePathToolTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly IAITool _tool;

    public GetContentTreePathToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new GetContentTreePathTool(_umbracoContextAccessorMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        // Arrange
        var args = new GetContentTreePathArgs(Guid.Empty);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTreePathResult>();
        var pathResult = (GetContentTreePathResult)result;
        pathResult.Success.ShouldBeFalse();
        pathResult.Message.ShouldContain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetContentTreePathArgs(Guid.NewGuid());
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext?>.IsAny))
            .Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTreePathResult>();
        var pathResult = (GetContentTreePathResult)result;
        pathResult.Success.ShouldBeFalse();
        pathResult.Message.ShouldContain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentKey_ReturnsNotFound()
    {
        // Arrange
        var key = Guid.NewGuid();
        var args = new GetContentTreePathArgs(key);

        var contentCacheMock = new Mock<IPublishedContentCache>();
        contentCacheMock.Setup(x => x.GetById(key)).Returns((IPublishedContent?)null);

        var umbracoContextMock = new Mock<IUmbracoContext>();
        umbracoContextMock.Setup(x => x.Content).Returns(contentCacheMock.Object);

        IUmbracoContext? ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out ctx))
            .Returns(true);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTreePathResult>();
        var pathResult = (GetContentTreePathResult)result;
        pathResult.Success.ShouldBeFalse();
        pathResult.Message.ShouldContain("not found");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("ancestor");
    }
}
