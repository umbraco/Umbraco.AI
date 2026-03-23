using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetContentTypeSchemaToolTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly Mock<IPublishedContentTypeCache> _publishedContentTypeCacheMock;
    private readonly IAITool _tool;

    public GetContentTypeSchemaToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _publishedContentTypeCacheMock = new Mock<IPublishedContentTypeCache>();
        _tool = new GetContentTypeSchemaTool(_umbracoContextAccessorMock.Object, _publishedContentTypeCacheMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoKeyOrAlias_ReturnsError()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs();

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("must be provided");
        schemaResult.Schema.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(ContentKey: Guid.NewGuid());
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out It.Ref<IUmbracoContext?>.IsAny))
            .Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentKey_ReturnsNotFound()
    {
        // Arrange
        var key = Guid.NewGuid();
        var args = new GetContentTypeSchemaArgs(ContentKey: key);

        var contentCacheMock = new Mock<IPublishedContentCache>();
        contentCacheMock.Setup(x => x.GetById(key)).Returns((IPublishedContent?)null);

        var mediaCacheMock = new Mock<IPublishedMediaCache>();
        mediaCacheMock.Setup(x => x.GetById(key)).Returns((IPublishedContent?)null);

        var umbracoContextMock = new Mock<IUmbracoContext>();
        umbracoContextMock.Setup(x => x.Content).Returns(contentCacheMock.Object);
        umbracoContextMock.Setup(x => x.Media).Returns(mediaCacheMock.Object);

        IUmbracoContext? ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(x => x.TryGetUmbracoContext(out ctx))
            .Returns(true);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<GetContentTypeSchemaResult>();
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentAlias_ReturnsNotFound()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(ContentTypeAlias: "nonExistentType");

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
    public async Task ExecuteAsync_WithAlias_PrefersAliasOverKey()
    {
        // Arrange - provide both, alias should take priority
        var args = new GetContentTypeSchemaArgs(ContentKey: Guid.NewGuid(), ContentTypeAlias: "blogPost");

        _publishedContentTypeCacheMock
            .Setup(x => x.Get(It.IsAny<PublishedItemType>(), "blogPost"))
            .Returns((IPublishedContentType?)null);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert - should attempt alias lookup, not key lookup
        var schemaResult = (GetContentTypeSchemaResult)result;
        schemaResult.Success.ShouldBeFalse();
        schemaResult.Message.ShouldContain("blogPost");
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
