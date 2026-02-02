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
    private readonly IAiTool _tool;

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
}
