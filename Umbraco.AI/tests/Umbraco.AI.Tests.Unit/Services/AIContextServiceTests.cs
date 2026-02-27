using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIContextServiceTests
{
    private readonly Mock<IAIContextRepository> _repositoryMock;
    private readonly Mock<IAIEntityVersionService> _versionServiceMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly AIContextService _service;

    public AIContextServiceTests()
    {
        _repositoryMock = new Mock<IAIContextRepository>();
        _versionServiceMock = new Mock<IAIEntityVersionService>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _service = new AIContextService(_repositoryMock.Object, _versionServiceMock.Object, _eventAggregatorMock.Object);
    }

    #region GetContextAsync

    [Fact]
    public async Task GetContextAsync_WithExistingId_ReturnsContext()
    {
        // Arrange
        var contextId = Guid.NewGuid();
        var context = new AIContextBuilder()
            .WithId(contextId)
            .WithAlias("test-context")
            .WithName("Test Context")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(contextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _service.GetContextAsync(contextId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(contextId);
        result.Alias.ShouldBe("test-context");
    }

    [Fact]
    public async Task GetContextAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var contextId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(contextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIContext?)null);

        // Act
        var result = await _service.GetContextAsync(contextId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetContextByAliasAsync

    [Fact]
    public async Task GetContextByAliasAsync_WithExistingAlias_ReturnsContext()
    {
        // Arrange
        var context = new AIContextBuilder()
            .WithAlias("test-context")
            .WithName("Test Context")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("test-context", It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _service.GetContextByAliasAsync("test-context");

        // Assert
        result.ShouldNotBeNull();
        result!.Alias.ShouldBe("test-context");
    }

    [Fact]
    public async Task GetContextByAliasAsync_WithNonExistingAlias_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByAliasAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIContext?)null);

        // Act
        var result = await _service.GetContextByAliasAsync("non-existent");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetContextsAsync

    [Fact]
    public async Task GetContextsAsync_ReturnsAllContexts()
    {
        // Arrange
        var contexts = new List<AIContext>
        {
            new AIContextBuilder().WithAlias("context-1").Build(),
            new AIContextBuilder().WithAlias("context-2").Build(),
            new AIContextBuilder().WithAlias("context-3").Build()
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(contexts);

        // Act
        var result = await _service.GetContextsAsync();

        // Assert
        result.Count().ShouldBe(3);
    }

    [Fact]
    public async Task GetContextsAsync_WithNoContexts_ReturnsEmptyCollection()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AIContext>());

        // Act
        var result = await _service.GetContextsAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetContextsPagedAsync

    [Fact]
    public async Task GetContextsPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var contexts = new List<AIContext>
        {
            new AIContextBuilder().WithAlias("context-1").Build(),
            new AIContextBuilder().WithAlias("context-2").Build()
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((contexts.AsEnumerable(), 5));

        // Act
        var (items, total) = await _service.GetContextsPagedAsync(skip: 0, take: 10);

        // Assert
        items.Count().ShouldBe(2);
        total.ShouldBe(5);
    }

    [Fact]
    public async Task GetContextsPagedAsync_WithFilter_PassesFilterToRepository()
    {
        // Arrange
        var contexts = new List<AIContext>
        {
            new AIContextBuilder().WithAlias("brand-context").WithName("Brand Context").Build()
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync("brand", 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((contexts.AsEnumerable(), 1));

        // Act
        var (items, total) = await _service.GetContextsPagedAsync(filter: "brand");

        // Assert
        items.Count().ShouldBe(1);
        total.ShouldBe(1);
        _repositoryMock.Verify(x => x.GetPagedAsync("brand", 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SaveContextAsync

    [Fact]
    public async Task SaveContextAsync_SavesAndReturnsContext()
    {
        // Arrange
        var context = new AIContextBuilder()
            .WithAlias("new-context")
            .WithName("New Context")
            .Build();

        _repositoryMock
            .Setup(x => x.SaveAsync(context, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _service.SaveContextAsync(context);

        // Assert
        result.ShouldNotBeNull();
        result.Alias.ShouldBe("new-context");
        _repositoryMock.Verify(x => x.SaveAsync(context, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveContextAsync_WithResources_SavesContextWithResources()
    {
        // Arrange
        var resource = new AIContextResourceBuilder()
            .WithName("Brand Voice")
            .AsBrandVoice()
            .Build();

        var context = new AIContextBuilder()
            .WithAlias("context-with-resources")
            .WithName("Context with Resources")
            .WithResources(resource)
            .Build();

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIContext>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);

        // Act
        var result = await _service.SaveContextAsync(context);

        // Assert
        result.Resources.ShouldNotBeEmpty();
        result.Resources.Count.ShouldBe(1);
    }

    #endregion

    #region DeleteContextAsync

    [Fact]
    public async Task DeleteContextAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var contextId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.DeleteAsync(contextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteContextAsync(contextId);

        // Assert
        result.ShouldBeTrue();
        _repositoryMock.Verify(x => x.DeleteAsync(contextId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteContextAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var contextId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.DeleteAsync(contextId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteContextAsync(contextId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
