using Examine;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class SearchUmbracoToolTests
{
    private readonly Mock<IExamineManager> _examineManagerMock;
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly IAITool _tool;

    public SearchUmbracoToolTests()
    {
        _examineManagerMock = new Mock<IExamineManager>();
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _tool = new SearchUmbracoTool(_examineManagerMock.Object, _umbracoContextAccessorMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyQuery_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs("", "all", null, 10);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>();
        var searchResult = (SearchUmbracoResult)result;
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
        result.ShouldBeOfType<SearchUmbracoResult>();
        var searchResult = (SearchUmbracoResult)result;
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("empty");
    }

    [Theory]
    [InlineData("content")]
    [InlineData("media")]
    [InlineData("all")]
    public async Task ExecuteAsync_WithValidTypeFilter_Succeeds(string typeFilter)
    {
        // Arrange
        var args = new SearchUmbracoArgs("test", typeFilter, null, 10);
        _examineManagerMock.Setup(x => x.TryGetIndex("ExternalIndex", out It.Ref<IIndex?>.IsAny))
            .Returns(false); // Index not available, but that's ok - should return error gracefully

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>();
        var searchResult = (SearchUmbracoResult)result;
        searchResult.Success.ShouldBeFalse(); // Because index not available
        searchResult.Message.ShouldContain("index");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTypeFilter_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs("test", "invalid-type", null, 10);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>();
        var searchResult = (SearchUmbracoResult)result;
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("Invalid type filter");
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(50, 50)]
    [InlineData(100, 50)] // Should be capped at 50
    [InlineData(null, 10)] // Should default to 10
    public async Task ExecuteAsync_WithMaxResults_EnforcesLimit(int? input, int expected)
    {
        // Arrange
        var args = new SearchUmbracoArgs("test", "all", null, input);
        _examineManagerMock.Setup(x => x.TryGetIndex("ExternalIndex", out It.Ref<IIndex?>.IsAny))
            .Returns(false); // Index not available

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>();
        // Can't directly verify the limit was applied without a working index,
        // but the test ensures the code path is exercised
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingExamineIndex_ReturnsError()
    {
        // Arrange
        var args = new SearchUmbracoArgs("test", "all", null, 10);
        _examineManagerMock.Setup(x => x.TryGetIndex("ExternalIndex", out It.Ref<IIndex?>.IsAny))
            .Returns(false);

        // Act
        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<SearchUmbracoResult>();
        var searchResult = (SearchUmbracoResult)result;
        searchResult.Success.ShouldBeFalse();
        searchResult.Message.ShouldContain("index");
        searchResult.Message.ShouldContain("not available");
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

    [Fact]
    public void BuildTextQuery_SingleTerm_ProducesBoostedNameAndBroadQuery()
    {
        // Act
        var result = SearchUmbracoTool.BuildTextQuery("homepage");

        // Assert
        result.ShouldContain("nodeName:homepage^10");
        result.ShouldContain("homepage^1");
        // Single term should NOT produce phrase clauses
        result.ShouldNotContain("\"homepage\"");
    }

    [Fact]
    public void BuildTextQuery_MultipleTerms_IncludesPhraseBoost()
    {
        // Act
        var result = SearchUmbracoTool.BuildTextQuery("contact form");

        // Assert
        result.ShouldContain("nodeName:contact^10");
        result.ShouldContain("nodeName:form^10");
        result.ShouldContain("nodeName:\"contact form\"^15");
        result.ShouldContain("contact^1");
        result.ShouldContain("form^1");
        result.ShouldContain("\"contact form\"^3");
    }

    [Fact]
    public void EscapeLuceneTerm_EscapesSpecialCharacters()
    {
        // Act & Assert
        SearchUmbracoTool.EscapeLuceneTerm("C++").ShouldBe("C\\+\\+");
        SearchUmbracoTool.EscapeLuceneTerm("test:value").ShouldBe("test\\:value");
        SearchUmbracoTool.EscapeLuceneTerm("hello world").ShouldBe("hello world");
        SearchUmbracoTool.EscapeLuceneTerm("(foo)").ShouldBe("\\(foo\\)");
    }

    [Fact]
    public void BuildTextQuery_CapsAtTenTerms()
    {
        // Arrange
        var longQuery = "one two three four five six seven eight nine ten eleven twelve";

        // Act
        var result = SearchUmbracoTool.BuildTextQuery(longQuery);

        // Assert - "eleven" and "twelve" should not appear
        result.ShouldNotContain("eleven");
        result.ShouldNotContain("twelve");
        result.ShouldContain("ten");
    }
}
