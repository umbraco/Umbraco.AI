using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetUmbracoContentChildrenToolTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly IAITool _tool;

    public GetUmbracoContentChildrenToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new GetUmbracoContentChildrenTool(_umbracoContextAccessorMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyParentKey_ReturnsError()
    {
        // Arrange
        var args = new GetUmbracoContentChildrenArgs(Guid.Empty);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentChildrenResult>();
        var childrenResult = (GetUmbracoContentChildrenResult)result;
        childrenResult.Success.ShouldBeFalse();
        childrenResult.Message.ShouldContain("empty");
        childrenResult.Children.ShouldBeEmpty();
        childrenResult.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetUmbracoContentChildrenArgs(Guid.NewGuid());
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext?>.IsAny))
            .Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentChildrenResult>();
        var childrenResult = (GetUmbracoContentChildrenResult)result;
        childrenResult.Success.ShouldBeFalse();
        childrenResult.Message.ShouldContain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentParent_ReturnsNotFound()
    {
        // Arrange
        var parentKey = Guid.NewGuid();
        var args = new GetUmbracoContentChildrenArgs(parentKey);

        var contentCacheMock = new Mock<IPublishedContentCache>();
        contentCacheMock.Setup(x => x.GetById(parentKey)).Returns((IPublishedContent?)null);

        var umbracoContextMock = new Mock<IUmbracoContext>();
        umbracoContextMock.Setup(x => x.Content).Returns(contentCacheMock.Object);

        IUmbracoContext? ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out ctx))
            .Returns(true);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentChildrenResult>();
        var childrenResult = (GetUmbracoContentChildrenResult)result;
        childrenResult.Success.ShouldBeFalse();
        childrenResult.Message.ShouldContain("not found");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("child");
    }
}
