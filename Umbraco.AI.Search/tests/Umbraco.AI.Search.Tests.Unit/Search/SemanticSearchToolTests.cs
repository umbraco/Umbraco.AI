using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.Search;
using Umbraco.AI.Search.Core.Tools;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit.Search;

/// <summary>
/// semantic_search only returns items the acting user can access, and nothing without a user.
/// </summary>
public class SemanticSearchToolTests
{
    private static readonly Guid SourceKey = Guid.NewGuid();
    private static readonly Guid VisibleKey = Guid.NewGuid();
    private static readonly Guid HiddenKey = Guid.NewGuid();

    private readonly Mock<IAIVectorStore> _vectorStore = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();
    private readonly Mock<IUmbracoReadAuthorizer> _readAuthorizer = new();
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessor = new();

    public SemanticSearchToolTests()
    {
        // "Find similar to SourceKey" returns one item the user can see and one they can't.
        _vectorStore
            .Setup(x => x.GetVectorsByDocumentAsync(It.IsAny<string>(), SourceKey.ToString(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AIVectorEntry(SourceKey.ToString(), null, 0, new float[] { 1f, 0f }, null)]);
        _vectorStore
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AIVectorSearchResult(VisibleKey.ToString(), 0.9, null),
                new AIVectorSearchResult(HiddenKey.ToString(), 0.8, null),
            ]);

        var contentCache = new Mock<IPublishedContentCache>();
        contentCache.Setup(x => x.GetById(VisibleKey)).Returns(Content(VisibleKey, "Visible", "-1,1100"));
        contentCache.Setup(x => x.GetById(HiddenKey)).Returns(Content(HiddenKey, "Hidden", "-1,1200"));
        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.SetupGet(x => x.Content).Returns(contentCache.Object);
        var context = umbracoContext.Object;
        _umbracoContextAccessor.Setup(x => x.TryGetUmbracoContext(out context)).Returns(true);

        _readAuthorizer.Setup(x => x.CanSeeContentInTreeAsync(VisibleKey, "-1,1100")).ReturnsAsync(true);
        _readAuthorizer.Setup(x => x.CanSeeContentInTreeAsync(HiddenKey, "-1,1200")).ReturnsAsync(false);
    }

    [Fact]
    public async Task ItemsTheUserCannotSee_AreLeftOut()
    {
        // (Building a returned item needs the URL provider, which isn't available in a unit test, so this
        // checks the filter by denying every match; the live demo-site check covers returned items.)
        WithUser(new Mock<IUser>().Object);
        _readAuthorizer.Setup(x => x.CanSeeContentInTreeAsync(VisibleKey, "-1,1100")).ReturnsAsync(false);

        var result = await SearchAsync();

        result.Success.ShouldBeTrue();
        result.Results.ShouldBeEmpty();
        _readAuthorizer.Verify(x => x.CanSeeContentInTreeAsync(VisibleKey, "-1,1100"), Times.Once);
        _readAuthorizer.Verify(x => x.CanSeeContentInTreeAsync(HiddenKey, "-1,1200"), Times.Once);
    }

    [Fact]
    public async Task WithNoActingUser_ReturnsNothing()
    {
        WithUser(null);

        var result = await SearchAsync();

        result.Success.ShouldBeFalse();
        result.Results.ShouldBeEmpty();
        _vectorStore.Verify(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<float>>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<SemanticSearchResult> SearchAsync()
    {
        var searcher = new AIVectorSearcher(
            _vectorStore.Object,
            Mock.Of<IAIEmbeddingService>(),
            Options.Create(new AIVectorSearchOptions()),
            NullLogger<AIVectorSearcher>.Instance);

        IAITool tool = new SemanticSearchTool(
            searcher,
            _vectorStore.Object,
            _umbracoContextAccessor.Object,
            _securityAccessor.Object,
            _readAuthorizer.Object,
            NullLogger<SemanticSearchTool>.Instance);

        var result = await tool.ExecuteAsync(new SemanticSearchArgs(DocumentId: SourceKey.ToString()), CancellationToken.None);
        return result.ShouldBeOfType<SemanticSearchResult>();
    }

    private void WithUser(IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(user);
        _securityAccessor.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);
    }

    private static IPublishedContent Content(Guid key, string name, string path)
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Alias).Returns("page");
        var content = new Mock<IPublishedContent>();
        content.SetupGet(x => x.Key).Returns(key);
        content.SetupGet(x => x.Name).Returns(name);
        content.SetupGet(x => x.Path).Returns(path);
        content.SetupGet(x => x.ContentType).Returns(contentType.Object);
        return content.Object;
    }
}
