using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentByRouteToolTests
{
    private readonly Mock<IUmbracoContextFactory> _umbracoContextFactoryMock;
    private readonly Mock<IDocumentUrlService> _documentUrlServiceMock;
    private readonly IAITool _tool;

    public GetContentByRouteToolTests()
    {
        _umbracoContextFactoryMock = new Mock<IUmbracoContextFactory>();
        _documentUrlServiceMock = new Mock<IDocumentUrlService>();
        _tool = new GetContentByRouteTool(_umbracoContextFactoryMock.Object, _documentUrlServiceMock.Object);
    }

    /// <summary>
    /// EnsureUmbracoContext() never fails to produce a context — unlike the old TryGetUmbracoContext
    /// check this replaced, it either reuses an ambient one or creates a fresh one (which is exactly
    /// what makes this tool work from Umbraco.Automate's background dispatcher, which has no ambient
    /// context). Tests below set up the context this factory hands back.
    /// </summary>
    private void SetUpUmbracoContext(IUmbracoContext umbracoContext)
    {
        var reference = new UmbracoContextReference(umbracoContext, false, Mock.Of<IUmbracoContextAccessor>());
        _umbracoContextFactoryMock.Setup(x => x.EnsureUmbracoContext()).Returns(reference);
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
        _umbracoContextFactoryMock.Verify(x => x.EnsureUmbracoContext(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentRoute_ReturnsNotFound()
    {
        // Arrange
        var args = new GetContentByRouteArgs("/non-existent-page");

        SetUpUmbracoContext(Mock.Of<IUmbracoContext>());

        _documentUrlServiceMock
            .Setup(x => x.GetDocumentKeyByRoute("/non-existent-page", string.Empty, null, false))
            .Returns((Guid?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetUmbracoContentResult>();
        var contentResult = (GetUmbracoContentResult)result;
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("No published content was found");
    }

    [Fact]
    public async Task ExecuteAsync_WithRouteWithoutLeadingSlash_NormalizesRoute()
    {
        // Arrange
        var args = new GetContentByRouteArgs("about-us");

        SetUpUmbracoContext(Mock.Of<IUmbracoContext>());

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
