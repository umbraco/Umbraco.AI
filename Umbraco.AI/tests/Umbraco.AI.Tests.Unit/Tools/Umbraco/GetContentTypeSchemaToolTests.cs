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
    private readonly IAITool _tool;

    public GetContentTypeSchemaToolTests()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new GetContentTypeSchemaTool(_umbracoContextAccessorMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(Guid.Empty);

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
    public async Task ExecuteAsync_WithNoUmbracoContext_ReturnsError()
    {
        // Arrange
        var args = new GetContentTypeSchemaArgs(Guid.NewGuid());
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
        var args = new GetContentTypeSchemaArgs(key);

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
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("schema");
    }
}
