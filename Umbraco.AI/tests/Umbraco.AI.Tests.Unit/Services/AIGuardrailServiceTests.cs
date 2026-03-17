using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIGuardrailServiceTests
{
    private readonly Mock<IAIGuardrailRepository> _repositoryMock;
    private readonly Mock<IAIEntityVersionService> _versionServiceMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly AIGuardrailService _service;

    public AIGuardrailServiceTests()
    {
        _repositoryMock = new Mock<IAIGuardrailRepository>();
        _versionServiceMock = new Mock<IAIEntityVersionService>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _service = new AIGuardrailService(_repositoryMock.Object, _versionServiceMock.Object, _eventAggregatorMock.Object);
    }

    #region GetGuardrailAsync

    [Fact]
    public async Task GetGuardrailAsync_WithExistingId_ReturnsGuardrail()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();
        var guardrail = new AIGuardrailBuilder()
            .WithId(guardrailId)
            .WithAlias("test-guardrail")
            .WithName("Test Guardrail")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(guardrailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.GetGuardrailAsync(guardrailId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(guardrailId);
        result.Alias.ShouldBe("test-guardrail");
    }

    [Fact]
    public async Task GetGuardrailAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(guardrailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        // Act
        var result = await _service.GetGuardrailAsync(guardrailId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetGuardrailByAliasAsync

    [Fact]
    public async Task GetGuardrailByAliasAsync_WithExistingAlias_ReturnsGuardrail()
    {
        // Arrange
        var guardrail = new AIGuardrailBuilder()
            .WithAlias("content-safety")
            .WithName("Content Safety")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("content-safety", It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.GetGuardrailByAliasAsync("content-safety");

        // Assert
        result.ShouldNotBeNull();
        result!.Alias.ShouldBe("content-safety");
    }

    [Fact]
    public async Task GetGuardrailByAliasAsync_WithNonExistingAlias_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByAliasAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        // Act
        var result = await _service.GetGuardrailByAliasAsync("non-existent");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetGuardrailsAsync

    [Fact]
    public async Task GetGuardrailsAsync_ReturnsAllGuardrails()
    {
        // Arrange
        var guardrails = new List<AIGuardrail>
        {
            new AIGuardrailBuilder().WithAlias("guardrail-1").Build(),
            new AIGuardrailBuilder().WithAlias("guardrail-2").Build(),
            new AIGuardrailBuilder().WithAlias("guardrail-3").Build()
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrails);

        // Act
        var result = await _service.GetGuardrailsAsync();

        // Assert
        result.Count().ShouldBe(3);
    }

    [Fact]
    public async Task GetGuardrailsAsync_WithNoGuardrails_ReturnsEmptyCollection()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AIGuardrail>());

        // Act
        var result = await _service.GetGuardrailsAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region GetGuardrailsPagedAsync

    [Fact]
    public async Task GetGuardrailsPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        var guardrails = new List<AIGuardrail>
        {
            new AIGuardrailBuilder().WithAlias("guardrail-1").Build(),
            new AIGuardrailBuilder().WithAlias("guardrail-2").Build()
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync(null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((guardrails.AsEnumerable(), 5));

        // Act
        var (items, total) = await _service.GetGuardrailsPagedAsync(skip: 0, take: 10);

        // Assert
        items.Count().ShouldBe(2);
        total.ShouldBe(5);
    }

    [Fact]
    public async Task GetGuardrailsPagedAsync_WithFilter_PassesFilterToRepository()
    {
        // Arrange
        var guardrails = new List<AIGuardrail>
        {
            new AIGuardrailBuilder().WithAlias("content-safety").WithName("Content Safety").Build()
        };

        _repositoryMock
            .Setup(x => x.GetPagedAsync("content", 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((guardrails.AsEnumerable(), 1));

        // Act
        var (items, total) = await _service.GetGuardrailsPagedAsync(filter: "content");

        // Assert
        items.Count().ShouldBe(1);
        total.ShouldBe(1);
        _repositoryMock.Verify(x => x.GetPagedAsync("content", 0, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SaveGuardrailAsync

    [Fact]
    public async Task SaveGuardrailAsync_SavesAndReturnsGuardrail()
    {
        // Arrange
        var guardrail = new AIGuardrailBuilder()
            .WithAlias("new-guardrail")
            .WithName("New Guardrail")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("new-guardrail", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        _repositoryMock
            .Setup(x => x.SaveAsync(guardrail, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.SaveGuardrailAsync(guardrail);

        // Assert
        result.ShouldNotBeNull();
        result.Alias.ShouldBe("new-guardrail");
        _repositoryMock.Verify(x => x.SaveAsync(guardrail, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveGuardrailAsync_WithRules_SavesGuardrailWithRules()
    {
        // Arrange
        var rule = new AIGuardrailRuleBuilder()
            .WithEvaluatorId("pii")
            .WithName("PII Detection")
            .AsPostGenerate()
            .AsBlock()
            .Build();

        var guardrail = new AIGuardrailBuilder()
            .WithAlias("guardrail-with-rules")
            .WithName("Guardrail with Rules")
            .WithRules(rule)
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("guardrail-with-rules", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIGuardrail>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.SaveGuardrailAsync(guardrail);

        // Assert
        result.Rules.ShouldNotBeEmpty();
        result.Rules.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveGuardrailAsync_WithDuplicateAlias_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingGuardrail = new AIGuardrailBuilder()
            .WithAlias("existing-alias")
            .Build();

        var newGuardrail = new AIGuardrailBuilder()
            .WithAlias("existing-alias")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("existing-alias", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGuardrail);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.SaveGuardrailAsync(newGuardrail));
    }

    [Fact]
    public async Task SaveGuardrailAsync_WithEmptyId_GeneratesNewId()
    {
        // Arrange
        var guardrail = new AIGuardrailBuilder()
            .WithId(Guid.Empty)
            .WithAlias("new-guardrail")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("new-guardrail", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIGuardrail>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail g, Guid? _, CancellationToken _) => g);

        // Act
        var result = await _service.SaveGuardrailAsync(guardrail);

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
    }

    #endregion

    #region DeleteGuardrailAsync

    [Fact]
    public async Task DeleteGuardrailAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.DeleteAsync(guardrailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteGuardrailAsync(guardrailId);

        // Assert
        result.ShouldBeTrue();
        _repositoryMock.Verify(x => x.DeleteAsync(guardrailId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGuardrailAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.DeleteAsync(guardrailId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteGuardrailAsync(guardrailId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GuardrailAliasExistsAsync

    [Fact]
    public async Task GuardrailAliasExistsAsync_WithExistingAlias_ReturnsTrue()
    {
        // Arrange
        var guardrail = new AIGuardrailBuilder()
            .WithAlias("existing-alias")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("existing-alias", It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.GuardrailAliasExistsAsync("existing-alias");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GuardrailAliasExistsAsync_WithNonExistingAlias_ReturnsFalse()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByAliasAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIGuardrail?)null);

        // Act
        var result = await _service.GuardrailAliasExistsAsync("non-existent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GuardrailAliasExistsAsync_WithExcludeId_ExcludesMatchingGuardrail()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();
        var guardrail = new AIGuardrailBuilder()
            .WithId(guardrailId)
            .WithAlias("existing-alias")
            .Build();

        _repositoryMock
            .Setup(x => x.GetByAliasAsync("existing-alias", It.IsAny<CancellationToken>()))
            .ReturnsAsync(guardrail);

        // Act
        var result = await _service.GuardrailAliasExistsAsync("existing-alias", guardrailId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
