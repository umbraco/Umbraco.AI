using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetUmbracoContentToolTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly IAITool _tool;

    public GetUmbracoContentToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new GetUmbracoContentTool(_umbracoContextAccessorMock.Object);
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
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetUmbracoContentArgs(Guid.NewGuid());
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext?>.IsAny))
            .Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentKey_ReturnsNotFound()
    {
        // Arrange
        var key = Guid.NewGuid();
        var args = new GetUmbracoContentArgs(key);

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
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("not found");
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
