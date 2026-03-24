using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentByRouteToolTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly Mock<IDocumentUrlService> _documentUrlServiceMock;
    private readonly IAITool _tool;

    public GetContentByRouteToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _documentUrlServiceMock = new Mock<IDocumentUrlService>();
        _tool = new GetContentByRouteTool(_umbracoContextAccessorMock.Object, _documentUrlServiceMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithEmptyRoute_ReturnsError(string? route)
    {
        // Arrange
        var args = new GetContentByRouteArgs(route!);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetContentByRouteArgs("/about-us");
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
    public async Task ExecuteAsync_WithNonExistentRoute_ReturnsNotFound()
    {
        // Arrange
        var args = new GetContentByRouteArgs("/non-existent-page");

        var umbracoContextMock = new Mock<IUmbracoContext>();
        IUmbracoContext? ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out ctx))
            .Returns(true);

        _documentUrlServiceMock
            .Setup(x => x.GetDocumentKeyByRoute("/non-existent-page", string.Empty, null, false))
            .Returns((Guid?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithRouteWithoutLeadingSlash_NormalizesRoute()
    {
        // Arrange
        var args = new GetContentByRouteArgs("about-us");

        var umbracoContextMock = new Mock<IUmbracoContext>();
        IUmbracoContext? ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out ctx))
            .Returns(true);

        // Should be called with normalized route (leading slash added)
        _documentUrlServiceMock
            .Setup(x => x.GetDocumentKeyByRoute("/about-us", string.Empty, null, false))
            .Returns((Guid?)null);

        // Act
        await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert - verify the route was normalized
        _documentUrlServiceMock.Verify(
            x => x.GetDocumentKeyByRoute("/about-us", string.Empty, null, false),
            Times.Once);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("URL");
    }
}
