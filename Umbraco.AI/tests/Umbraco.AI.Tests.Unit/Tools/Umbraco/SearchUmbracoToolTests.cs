using Moq;
using Umbraco.Cms.Core;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Search;
using Umbraco.Cms.Core.Search.Querying;
using Umbraco.Cms.Core.Search.Querying.Faceting;
using Umbraco.Cms.Core.Search.Querying.Filtering;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class SearchUmbracoToolTests
{
    private const string PublishedContentIndex = "Umb_PublishedContent";
    private const string MediaIndex = "Umb_Media";

    private readonly Mock<ISearcherResolver> _searcherResolverMock = new();
    private readonly Mock<ISearcher> _searcherMock = new();
    private readonly Mock<ILanguageService> _languageServiceMock = new();
    private readonly Mock<IUmbracoContextFactory> _umbracoContextFactoryMock = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _backOfficeSecurityAccessorMock = new();
    private readonly IAITool _tool;

    public SearchUmbracoToolTests()
    {
        _searcherResolverMock.Setup(x => x.GetSearcher(It.IsAny<string>())).Returns(_searcherMock.Object);
        _searcherMock
            .Setup(x => x.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IEnumerable<Filter>?>(),
                It.IsAny<IEnumerable<Facet>?>(),
                It.IsAny<IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<AccessContext?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new SearchResult(0, [], []));

        SetLanguages("en-US");
        _umbracoContextFactoryMock
            .Setup(x => x.EnsureUmbracoContext())
            .Returns(new UmbracoContextReference(
                Mock.Of<IUmbracoContext>(),
                false,
                Mock.Of<IUmbracoContextAccessor>()));
        SetActingUser(allowedSections: ["content"]);

        _tool = new SearchUmbracoTool(
            _searcherResolverMock.Object,
            _languageServiceMock.Object,
            _umbracoContextFactoryMock.Object,
            _backOfficeSecurityAccessorMock.Object,
            TestReadAuthorizers.AllowAll().Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyQuery_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs("", "all", null, 10);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldNotBeNullOrEmpty();
        searchResult.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullQuery_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs(null!, "all", null, 10);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTypeFilter_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs("test", "invalid-type", null, 10);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("Invalid type filter");
    }

    [Theory]
    [InlineData("content")]
    [InlineData("media")]
    [InlineData("all")]
    public async Task ExecuteAsync_WithNoSearcherForTheIndex_ReturnsIndexNotAvailable(string typeFilter)
    {
        // Arrange
        _searcherResolverMock.Setup(x => x.GetSearcher(It.IsAny<string>())).Returns((ISearcher?)null);

        // Act
        var result = await _tool.ExecuteAsync(new SearchUmbracoArgs("test", typeFilter, null, 10), CancellationToken.None);

        // Assert
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("index");
        searchResult.Message.ShouldContain("not available");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoActingUser_ReturnsError_WithoutSearching()
    {
        // Arrange — the index exists, but nobody is signed in (e.g. a background run with no user).
        _backOfficeSecurityAccessorMock.SetupGet(x => x.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);

        // Act
        var result = await _tool.ExecuteAsync(new SearchUmbracoArgs("test", "all", null, 10), CancellationToken.None);

        // Assert
        var searchResult = result.ShouldBeOfType<SearchUmbracoResult>();
        searchResult.Success.ShouldBeFalse();
        searchResult.Results.ShouldBeEmpty();
        VerifySearchedIndexes();
    }

    [Theory]
    [InlineData("content", new[] { PublishedContentIndex })]
    [InlineData("media", new[] { MediaIndex })]
    [InlineData("all", new[] { PublishedContentIndex, MediaIndex })]
    public async Task ExecuteAsync_SearchesTheIndexesForTheTypeFilter(string typeFilter, string[] expectedIndexes)
    {
        // Act
        var result = await _tool.ExecuteAsync(new SearchUmbracoArgs("test", typeFilter, null, 10), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>().Success.ShouldBeTrue();
        VerifySearchedIndexes(expectedIndexes);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheQueryTextAsIs()
    {
        // Act
        await _tool.ExecuteAsync(new SearchUmbracoArgs("contact form", "content", null, 10), CancellationToken.None);

        // Assert
        _searcherMock.Verify(
            x => x.SearchAsync(PublishedContentIndex, "contact form", null, null, null, It.IsAny<string?>(), null, It.IsAny<AccessContext?>(), 0, It.IsAny<int>(), 0),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTags_FiltersOnAnyOfTheTags()
    {
        // Arrange
        IEnumerable<Filter>? captured = null;
        _searcherMock
            .Setup(x => x.SearchAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<IEnumerable<Filter>?>(),
                It.IsAny<IEnumerable<Facet>?>(),
                It.IsAny<IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<AccessContext?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Callback<string, string?, IEnumerable<Filter>?, IEnumerable<Facet>?, IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?, string?, string?, AccessContext?, int, int, int>(
                (_, _, filters, _, _, _, _, _, _, _, _) => captured = filters)
            .ReturnsAsync(new SearchResult(0, [], []));

        // Act
        await _tool.ExecuteAsync(new SearchUmbracoArgs("test", "content", ["news", "blog"], 10), CancellationToken.None);

        // Assert
        var filter = captured.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<KeywordFilter>();
        filter.FieldName.ShouldBe("Umb_Tags");
        filter.Values.ShouldBe(["news", "blog"]);
        filter.Negate.ShouldBeFalse();
    }

    [Theory]
    [InlineData(5, 25)]
    [InlineData(10, 50)]
    [InlineData(50, 250)]
    [InlineData(100, 250)] // Capped at 50 results, so at most 250 candidates
    [InlineData(null, 50)] // Defaults to 10 results
    public async Task ExecuteAsync_WithMaxResults_FetchesCappedCandidates(int? maxResults, int expectedTake)
    {
        // Act
        await _tool.ExecuteAsync(new SearchUmbracoArgs("test", "content", null, maxResults), CancellationToken.None);

        // Assert
        _searcherMock.Verify(
            x => x.SearchAsync(PublishedContentIndex, It.IsAny<string?>(), It.IsAny<IEnumerable<Filter>?>(), It.IsAny<IEnumerable<Facet>?>(), It.IsAny<IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<AccessContext?>(), 0, expectedTake, 0),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SearchesEveryCulture()
    {
        // Arrange
        SetLanguages("en-US", "da-DK");

        // Act
        await _tool.ExecuteAsync(new SearchUmbracoArgs("test", "content", null, 10), CancellationToken.None);

        // Assert
        foreach (var culture in new[] { "en-US", "da-DK" })
        {
            _searcherMock.Verify(
                x => x.SearchAsync(PublishedContentIndex, It.IsAny<string?>(), It.IsAny<IEnumerable<Filter>?>(), It.IsAny<IEnumerable<Facet>?>(), It.IsAny<IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?>(), culture, It.IsAny<string?>(), It.IsAny<AccessContext?>(), 0, It.IsAny<int>(), 0),
                Times.Once);
        }
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        // Act
        var description = _tool.Description;

        // Assert
        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("Search");
        description.ShouldContain("content");
        description.ShouldContain("media");
    }

    private void SetLanguages(params string[] isoCodes)
    {
        var languages = isoCodes
            .Select(code =>
            {
                var language = new Mock<ILanguage>();
                language.SetupGet(x => x.IsoCode).Returns(code);
                return language.Object;
            })
            .ToArray();
        _languageServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(languages);
    }

    private void SetActingUser(string[] allowedSections)
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.AllowedSections).Returns(allowedSections);
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(user.Object);
        _backOfficeSecurityAccessorMock.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);
    }

    private void VerifySearchedIndexes(params string[] expectedIndexes)
    {
        foreach (var index in new[] { PublishedContentIndex, MediaIndex })
        {
            _searcherMock.Verify(
                x => x.SearchAsync(index, It.IsAny<string?>(), It.IsAny<IEnumerable<Filter>?>(), It.IsAny<IEnumerable<Facet>?>(), It.IsAny<IEnumerable<global::Umbraco.Cms.Core.Search.Querying.Sorting.Sorter>?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<AccessContext?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                expectedIndexes.Contains(index) ? Times.AtLeastOnce() : Times.Never());
        }
    }
}
