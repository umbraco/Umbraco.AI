using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetUmbracoContentToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly Mock<IContentService> _contentServiceMock;
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly IAITool _tool;

    public GetUmbracoContentToolTests()
    {
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _contentServiceMock = new Mock<IContentService>();
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new GetUmbracoContentTool(
            _contentEditingServiceMock.Object,
            _contentServiceMock.Object,
            _umbracoContextAccessorMock.Object);

        // No Umbraco context available by default — the published-cache URL lookup itself relies on an
        // ambient StaticServiceProvider that isn't configured in a unit test, so every test here exercises
        // the unpublished/draft path deliberately, mirroring GetContentByRouteToolTests' own approach.
        _umbracoContextAccessorMock
            .Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext?>.IsAny))
            .Returns(false);
    }

    private static Mock<IContent> CreateContentMock(Guid key, string name, string contentTypeAlias, int level = 1)
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
        contentMock.Setup(x => x.Level).Returns(level);
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
        var contentResult = result.ShouldBeOfType<GetUmbracoContentResult>();
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
        var contentResult = result.ShouldBeOfType<GetUmbracoContentResult>();
        contentResult.Success.ShouldBeFalse();
        contentResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithDraftContent_ReturnsContentWithBreadcrumbAndParent_ButNoUrl()
    {
        // Arrange — proves get_umbraco_content can fetch content that has never been published, and
        // still resolves a real breadcrumb/parent (unlike the write tools' lean confirmation payload)
        // via IContentService.GetAncestors, which works regardless of publish state.
        var key = Guid.NewGuid();
        var args = new GetUmbracoContentArgs(key);
        var contentMock = CreateContentMock(key, "Draft Page", "page", level: 2);
        var rootMock = CreateContentMock(Guid.NewGuid(), "Home", "home", level: 1);

        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);
        _contentServiceMock.Setup(x => x.GetAncestors(contentMock.Object)).Returns([rootMock.Object]);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var contentResult = result.ShouldBeOfType<GetUmbracoContentResult>();
        contentResult.Success.ShouldBeTrue();
        contentResult.Content.ShouldNotBeNull();
        contentResult.Content!.Key.ShouldBe(key);
        contentResult.Content.Name.ShouldBe("Draft Page");
        contentResult.Content.Url.ShouldBeNull();
        contentResult.Content.Parent.ShouldNotBeNull();
        contentResult.Content.Parent!.Name.ShouldBe("Home");
        contentResult.Content.Path.ShouldBe("Home > Draft Page");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAncestors_ReturnsSelfOnlyBreadcrumbAndNullParent()
    {
        // Arrange — a root-level item has no ancestors at all.
        var key = Guid.NewGuid();
        var args = new GetUmbracoContentArgs(key);
        var contentMock = CreateContentMock(key, "Home", "home");

        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);
        _contentServiceMock.Setup(x => x.GetAncestors(contentMock.Object)).Returns([]);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var contentResult = result.ShouldBeOfType<GetUmbracoContentResult>();
        contentResult.Content!.Parent.ShouldBeNull();
        contentResult.Content.Path.ShouldBe("Home");
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
