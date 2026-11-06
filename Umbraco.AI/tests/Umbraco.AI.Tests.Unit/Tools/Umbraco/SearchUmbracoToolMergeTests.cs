using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.AI.Tests.Unit.Api.Management;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Search;
using Umbraco.Cms.Core.Search.Querying;
using Umbraco.Cms.Core.Search.Querying.Faceting;
using Umbraco.Cms.Core.Search.Querying.Filtering;
using Umbraco.Cms.Core.Search.Querying.Sorting;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

/// <summary>
/// Covers how <see cref="SearchUmbracoTool"/> turns searcher hits into results: merging across indexes,
/// cultures and words, bounding the work done, and filtering by the published cache and the authorizer.
/// </summary>
/// <remarks>
/// Building a result item uses the CMS's friendly <c>Url()</c>/<c>Ancestors()</c> extensions, which read
/// <see cref="StaticServiceProvider.Instance"/>, so this joins the collection that owns swapping it.
/// </remarks>
[Collection(nameof(StaticServiceProviderTestCollection))]
public class SearchUmbracoToolMergeTests : IDisposable
{
    private const string PublishedContentIndex = "Umb_PublishedContent";
    private const string MediaIndex = "Umb_Media";

    private readonly IServiceProvider? _originalServiceProvider = StaticServiceProvider.Instance;
    private readonly FakeSearcher _searcher = new();
    private readonly Mock<ILanguageService> _languageServiceMock = new();
    private readonly Mock<IPublishedContentCache> _contentCache = new();
    private readonly Mock<IPublishedMediaCache> _mediaCache = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();

    public SearchUmbracoToolMergeTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IPublishedUrlProvider>(x => x.GetUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<Uri?>()) == "/url"));
        services.AddSingleton(Mock.Of<IDocumentNavigationQueryService>());
        services.AddSingleton(Mock.Of<IMediaNavigationQueryService>());
        services.AddSingleton(Mock.Of<IPublishedContentStatusFilteringService>());
        services.AddSingleton(Mock.Of<IPublishedMediaStatusFilteringService>());
        StaticServiceProvider.Instance = services.BuildServiceProvider();

        SetLanguages("en-US");

        var umbracoContext = new Mock<IUmbracoContext>();
        umbracoContext.SetupGet(x => x.Content).Returns(_contentCache.Object);
        umbracoContext.SetupGet(x => x.Media).Returns(_mediaCache.Object);
        UmbracoContextFactory
            .Setup(x => x.EnsureUmbracoContext())
            .Returns(new UmbracoContextReference(umbracoContext.Object, false, Mock.Of<IUmbracoContextAccessor>()));

        var user = new Mock<IUser>();
        user.SetupGet(x => x.AllowedSections).Returns(["content"]);
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(user.Object);
        _securityAccessor.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);
    }

    private Mock<IUmbracoContextFactory> UmbracoContextFactory { get; } = new();

    public void Dispose() => StaticServiceProvider.Instance = _originalServiceProvider!;

    [Fact]
    public async Task InterleavesByRankAcrossCulturesAndIndexes()
    {
        // Arrange — list order is index, then culture: content en, content da, media en, media da.
        SetLanguages("en-US", "da-DK");
        var (a, b, c, d, m1, m2) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _searcher.Returns(PublishedContentIndex, "news", "en-US", a, b);
        _searcher.Returns(PublishedContentIndex, "news", "da-DK", c, d);
        _searcher.Returns(MediaIndex, "news", "en-US", m1);
        _searcher.Returns(MediaIndex, "news", "da-DK", m2);
        PublishContent(a, b, c, d);
        PublishMedia(m1, m2);

        // Act
        var result = await SearchAsync("news");

        // Assert
        result.Results.Select(x => x.Id).ShouldBe([a, c, m1, m2, b, d]);
    }

    [Fact]
    public async Task ReturnsAnItemOnceWhenItMatchesInSeveralCulturesAndIndexes()
    {
        // Arrange
        SetLanguages("en-US", "da-DK");
        var (shared, other) = (Guid.NewGuid(), Guid.NewGuid());
        _searcher.Returns(PublishedContentIndex, "news", "en-US", shared);
        _searcher.Returns(PublishedContentIndex, "news", "da-DK", shared, other);
        _searcher.Returns(MediaIndex, "news", "en-US", shared);
        PublishContent(shared, other);
        PublishMedia(shared);

        // Act
        var result = await SearchAsync("news");

        // Assert — the first list to return the key decides what kind of item it is.
        result.Results.Select(x => x.Id).ShouldBe([shared, other]);
        result.Results[0].Type.ShouldBe("content");
    }

    [Fact]
    public async Task SynthesizesDescendingScoresFromTheFinalOrder()
    {
        // Arrange — the middle hit is dropped (not in the published cache), so scores follow what is returned.
        var (a, gone, c) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _searcher.Returns(PublishedContentIndex, "news", "en-US", a, gone, c);
        PublishContent(a, c);

        // Act
        var result = await SearchAsync("news");

        // Assert
        result.Results.Select(x => x.Id).ShouldBe([a, c]);
        result.Results.Select(x => x.Score).ShouldBe([2d, 1d]);
    }

    [Fact]
    public async Task ResolvesMediaHitsFromTheMediaCacheAndAuthorizesThemAsMedia()
    {
        // Arrange
        var media = Guid.NewGuid();
        _searcher.Returns(MediaIndex, "logo", "en-US", media);
        PublishMedia(media);
        var authorizer = TestReadAuthorizers.AllowAll();

        // Act
        var result = await SearchAsync("logo", authorizer: authorizer.Object);

        // Assert
        var item = result.Results.ShouldHaveSingleItem();
        item.Id.ShouldBe(media);
        item.Type.ShouldBe("media");
        _mediaCache.Verify(x => x.GetById(media), Times.Once);
        _contentCache.Verify(x => x.GetById(It.IsAny<Guid>()), Times.Never);
        authorizer.Verify(x => x.CanReadMediaAsync(media), Times.Once);
        authorizer.Verify(x => x.CanSeeContentInTreeAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DropsHitsTheAuthorizerDenies()
    {
        // Arrange
        var (allowed, deniedContent, deniedMedia) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _searcher.Returns(PublishedContentIndex, "news", "en-US", deniedContent, allowed);
        _searcher.Returns(MediaIndex, "news", "en-US", deniedMedia);
        PublishContent(deniedContent, allowed);
        PublishMedia(deniedMedia);

        var authorizer = new Mock<IUmbracoReadAuthorizer>();
        authorizer.Setup(x => x.CanSeeContentInTreeAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync((Guid key, string _) => key == allowed);
        authorizer.Setup(x => x.CanReadMediaAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Act
        var result = await SearchAsync("news", authorizer: authorizer.Object);

        // Assert
        result.Results.Select(x => x.Id).ShouldBe([allowed]);
    }

    [Fact]
    public async Task CapsTheCandidatesThatAreCheckedAgainstTheCacheAndAuthorizer()
    {
        // Arrange — three cultures each return 250 distinct hits (750 in all) and the user can see none.
        SetLanguages("en-US", "da-DK", "de-DE");
        foreach (var culture in new[] { "en-US", "da-DK", "de-DE" })
        {
            var keys = Enumerable.Range(0, 250).Select(_ => Guid.NewGuid()).ToArray();
            _searcher.Returns(PublishedContentIndex, "news", culture, keys);
            PublishContent(keys);
        }

        var authorizer = new Mock<IUmbracoReadAuthorizer>();
        authorizer.Setup(x => x.CanSeeContentInTreeAsync(It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await SearchAsync("news", contentType: "content", maxResults: 50, authorizer: authorizer.Object);

        // Assert
        result.Results.ShouldBeEmpty();
        authorizer.Verify(x => x.CanSeeContentInTreeAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Exactly(250));
        _contentCache.Verify(x => x.GetById(It.IsAny<Guid>()), Times.Exactly(250));
    }

    [Fact]
    public async Task StopsCheckingCandidatesOnceMaxResultsAreFound()
    {
        // Arrange
        var keys = Enumerable.Range(0, 15).Select(_ => Guid.NewGuid()).ToArray();
        _searcher.Returns(PublishedContentIndex, "news", "en-US", keys);
        PublishContent(keys);

        // Act
        var result = await SearchAsync("news", contentType: "content", maxResults: 3);

        // Assert
        result.Results.Select(x => x.Id).ShouldBe(keys.Take(3));
        _contentCache.Verify(x => x.GetById(It.IsAny<Guid>()), Times.Exactly(3));
    }

    [Fact]
    public async Task WhenAllWordsFindTooLittle_SearchesEachWordAndAppendsNewHits()
    {
        // Arrange — the all-words search finds one page; each word on its own finds more.
        var (a, b, c) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _searcher.Returns(PublishedContentIndex, "red shoes", "en-US", a);
        _searcher.Returns(PublishedContentIndex, "red", "en-US", b, a);
        _searcher.Returns(PublishedContentIndex, "shoes", "en-US", c);
        PublishContent(a, b, c);

        // Act
        var result = await SearchAsync("red shoes", contentType: "content");

        // Assert — all-words hits come first, and the repeat of A is dropped.
        result.Results.Select(x => x.Id).ShouldBe([a, b, c]);
        _searcher.Queries.ShouldBe(["red shoes", "red", "shoes"]);
    }

    [Fact]
    public async Task WhenAllWordsFillMaxResults_DoesNotSearchEachWord()
    {
        // Arrange
        var a = Guid.NewGuid();
        _searcher.Returns(PublishedContentIndex, "red shoes", "en-US", a);
        PublishContent(a);

        // Act
        await SearchAsync("red shoes", contentType: "content", maxResults: 1);

        // Assert
        _searcher.Queries.ShouldBe(["red shoes"]);
    }

    [Fact]
    public async Task WithASingleWord_DoesNotRunAFallbackSearch()
    {
        // Act
        await SearchAsync("shoes", contentType: "content");

        // Assert
        _searcher.Queries.ShouldBe(["shoes"]);
    }

    [Fact]
    public async Task SearchesAtMostFiveDistinctWordsInTheFallback_BoundingTheSearcherCalls()
    {
        // Arrange — two indexes and two cultures, and a query of 8 words (one repeated) that finds nothing.
        SetLanguages("en-US", "da-DK");

        // Act
        await SearchAsync("a b c d e f g a");

        // Assert — 2 indexes x 2 cultures x (the whole query + 5 words) = 24 searcher calls.
        _searcher.Calls.Count.ShouldBe(24);
        _searcher.Queries.Distinct().ShouldBe(["a b c d e f g a", "a", "b", "c", "d", "e"]);
    }

    [Fact]
    public async Task SearchesWithoutAnAccessContext_SoProtectedContentIsNotReturned()
    {
        // Arrange
        SetLanguages("en-US", "da-DK");

        // Act
        await SearchAsync("red shoes");

        // Assert — a null access context only matches unprotected content in the CMS searcher.
        _searcher.Calls.ShouldNotBeEmpty();
        _searcher.Calls.ShouldAllBe(x => x.AccessContext == null);
    }

    private async Task<SearchUmbracoResult> SearchAsync(
        string query,
        string contentType = "all",
        int maxResults = 10,
        IUmbracoReadAuthorizer? authorizer = null)
    {
        var resolver = new Mock<ISearcherResolver>();
        resolver.Setup(x => x.GetSearcher(It.IsAny<string>())).Returns(_searcher);

        IAITool tool = new SearchUmbracoTool(
            resolver.Object,
            _languageServiceMock.Object,
            UmbracoContextFactory.Object,
            _securityAccessor.Object,
            authorizer ?? TestReadAuthorizers.AllowAll().Object);

        var result = await tool.ExecuteAsync(new SearchUmbracoArgs(query, contentType, null, maxResults), CancellationToken.None);
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeTrue(searchResult.Message);
        return searchResult;
    }

    private void SetLanguages(params string[] isoCodes)
    {
        var languages = isoCodes
            .Select(code => Mock.Of<ILanguage>(x => x.IsoCode == code))
            .ToArray();
        _languageServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(languages);
    }

    private void PublishContent(params Guid[] keys)
    {
        foreach (var key in keys)
        {
            _contentCache.Setup(x => x.GetById(key)).Returns(CreatePublishedItem(key, isMedia: false));
        }
    }

    private void PublishMedia(params Guid[] keys)
    {
        foreach (var key in keys)
        {
            _mediaCache.Setup(x => x.GetById(key)).Returns(CreatePublishedItem(key, isMedia: true));
        }
    }

    private static IPublishedContent CreatePublishedItem(Guid key, bool isMedia)
    {
        var itemType = isMedia ? PublishedItemType.Media : PublishedItemType.Content;
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Alias).Returns(isMedia ? "Image" : "page");
        contentType.SetupGet(x => x.ItemType).Returns(itemType);

        var item = new Mock<IPublishedContent>();
        item.SetupGet(x => x.Key).Returns(key);
        item.SetupGet(x => x.Name).Returns($"Item {key}");
        item.SetupGet(x => x.Path).Returns("-1,1");
        item.SetupGet(x => x.Level).Returns(1);
        item.SetupGet(x => x.ItemType).Returns(itemType);
        item.SetupGet(x => x.ContentType).Returns(contentType.Object);
        item.SetupGet(x => x.UpdateDate).Returns(new DateTime(2026, 1, 1));
        return item.Object;
    }

    /// <summary>
    /// A searcher that returns configured hits per index, query and culture (honouring <c>take</c>), and
    /// records every call.
    /// </summary>
    private sealed class FakeSearcher : ISearcher
    {
        private readonly Dictionary<(string Index, string Query, string? Culture), Guid[]> _hits = new();

        public List<(string Index, string? Query, string? Culture, AccessContext? AccessContext, int Take)> Calls { get; } = [];

        public IEnumerable<string?> Queries => Calls.Select(x => x.Query).Distinct();

        public void Returns(string index, string query, string culture, params Guid[] keys) => _hits[(index, query, culture)] = keys;

        public Task<SearchResult> SearchAsync(
            string indexAlias,
            string? query = null,
            IEnumerable<Filter>? filters = null,
            IEnumerable<Facet>? facets = null,
            IEnumerable<Sorter>? sorters = null,
            string? culture = null,
            string? segment = null,
            AccessContext? accessContext = null,
            int skip = 0,
            int take = 10,
            int maxSuggestions = 0)
        {
            Calls.Add((indexAlias, query, culture, accessContext, take));

            var objectType = indexAlias == MediaIndex ? UmbracoObjectTypes.Media : UmbracoObjectTypes.Document;
            Document[] documents = _hits.TryGetValue((indexAlias, query ?? string.Empty, culture), out var keys)
                ? keys.Take(take).Select(key => new Document(key, objectType)).ToArray()
                : [];

            return Task.FromResult(new SearchResult(documents.Length, documents, []));
        }
    }
}
