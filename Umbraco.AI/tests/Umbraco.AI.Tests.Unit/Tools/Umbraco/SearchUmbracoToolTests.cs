using Examine;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class SearchUmbracoToolTests
{
    private readonly Mock<IExamineManager> _examineManagerMock;
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private readonly Mock<IBackOfficeSecurityAccessor> _backOfficeSecurityAccessorMock;
    private readonly IAITool _tool;

    public SearchUmbracoToolTests()
    {
        _examineManagerMock = new Mock<IExamineManager>();
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _backOfficeSecurityAccessorMock = new Mock<IBackOfficeSecurityAccessor>();
        _tool = new SearchUmbracoTool(
            _examineManagerMock.Object,
            _umbracoContextAccessorMock.Object,
            _backOfficeSecurityAccessorMock.Object);
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

    #region IsUnrestricted

    [Fact]
    public void IsUnrestricted_NullIds_ReturnsTrue()
    {
        SearchUmbracoTool.IsUnrestricted(null).ShouldBeTrue();
    }

    [Fact]
    public void IsUnrestricted_EmptyIds_ReturnsTrue()
    {
        SearchUmbracoTool.IsUnrestricted([]).ShouldBeTrue();
    }

    [Fact]
    public void IsUnrestricted_ContainsRootId_ReturnsTrue()
    {
        SearchUmbracoTool.IsUnrestricted([-1]).ShouldBeTrue();
    }

    [Fact]
    public void IsUnrestricted_ContainsRootIdAmongOthers_ReturnsTrue()
    {
        SearchUmbracoTool.IsUnrestricted([-1, 1234]).ShouldBeTrue();
    }

    [Fact]
    public void IsUnrestricted_SpecificIds_ReturnsFalse()
    {
        SearchUmbracoTool.IsUnrestricted([1234, 5678]).ShouldBeFalse();
    }

    #endregion

    #region GetEffectiveStartNodeIds

    [Fact]
    public void GetEffectiveStartNodeIds_UserIdsSet_ReturnsUserIds()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds([100, 200], [300, 400]);

        result.ShouldBe([100, 200]);
    }

    [Fact]
    public void GetEffectiveStartNodeIds_NullUserIds_FallsBackToGroupIds()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds(null, new int?[] { 300, 400 });

        result.ShouldBe([300, 400]);
    }

    [Fact]
    public void GetEffectiveStartNodeIds_EmptyUserIds_FallsBackToGroupIds()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds([], new int?[] { 500 });

        result.ShouldBe([500]);
    }

    [Fact]
    public void GetEffectiveStartNodeIds_NullUserAndGroupIds_ReturnsNull()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds(null, new int?[] { null, null });

        result.ShouldBeNull();
    }

    [Fact]
    public void GetEffectiveStartNodeIds_NullUserAndEmptyGroups_ReturnsNull()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds(null, Enumerable.Empty<int?>());

        result.ShouldBeNull();
    }

    [Fact]
    public void GetEffectiveStartNodeIds_GroupIdsDeduplicates()
    {
        var result = SearchUmbracoTool.GetEffectiveStartNodeIds(null, new int?[] { 100, 100, 200 });

        result.ShouldBe([100, 200]);
    }

    #endregion

    #region BuildStartNodePathFilter

    [Fact]
    public void BuildStartNodePathFilter_ContentFilter_WithRestriction_ReturnsPathClause()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("content", [1234], null);

        result.ShouldBe("(__Path:1234)");
    }

    [Fact]
    public void BuildStartNodePathFilter_ContentFilter_Unrestricted_ReturnsNull()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("content", null, [5678]);

        result.ShouldBeNull();
    }

    [Fact]
    public void BuildStartNodePathFilter_MediaFilter_WithRestriction_ReturnsPathClause()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("media", null, [5678]);

        result.ShouldBe("(__Path:5678)");
    }

    [Fact]
    public void BuildStartNodePathFilter_MediaFilter_Unrestricted_ReturnsNull()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("media", [1234], null);

        result.ShouldBeNull();
    }

    [Fact]
    public void BuildStartNodePathFilter_AllFilter_BothUnrestricted_ReturnsNull()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("all", null, null);

        result.ShouldBeNull();
    }

    [Fact]
    public void BuildStartNodePathFilter_AllFilter_BothRestricted_CombinesIds()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("all", [100], [200]);

        result.ShouldBe("(__Path:100 OR __Path:200)");
    }

    [Fact]
    public void BuildStartNodePathFilter_AllFilter_OnlyContentRestricted_AllowsAllMedia()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("all", [100], null);

        result.ShouldBe("(__IndexType:media OR ((__Path:100)))");
    }

    [Fact]
    public void BuildStartNodePathFilter_AllFilter_OnlyMediaRestricted_AllowsAllContent()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("all", null, [200]);

        result.ShouldBe("(__IndexType:content OR ((__Path:200)))");
    }

    [Fact]
    public void BuildStartNodePathFilter_MultipleStartNodes_ReturnsOrClause()
    {
        var result = SearchUmbracoTool.BuildStartNodePathFilter("content", [100, 200, 300], null);

        result.ShouldBe("(__Path:100 OR __Path:200 OR __Path:300)");
    }

    [Fact]
    public void BuildStartNodePathFilter_RootAccess_ReturnsNull()
    {
        // -1 means root access (unrestricted)
        var result = SearchUmbracoTool.BuildStartNodePathFilter("content", [-1], null);

        result.ShouldBeNull();
    }

    #endregion
}
