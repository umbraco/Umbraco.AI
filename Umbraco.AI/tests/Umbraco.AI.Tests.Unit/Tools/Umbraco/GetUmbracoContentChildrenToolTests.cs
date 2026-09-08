using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class GetUmbracoContentChildrenToolTests
{
    private readonly Mock<IUmbracoContextFactory> _umbracoContextFactoryMock;
    private readonly IAITool _tool;

    public GetUmbracoContentChildrenToolTests()
    {
        _umbracoContextFactoryMock = new Mock<IUmbracoContextFactory>();
        _tool = new GetUmbracoContentChildrenTool(_umbracoContextFactoryMock.Object);
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
        _umbracoContextFactoryMock.Verify(x => x.EnsureUmbracoContext(), Times.Never);
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
        SetUpUmbracoContext(umbracoContextMock.Object);

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
