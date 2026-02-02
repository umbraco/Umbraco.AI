using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Persistence;
using Umbraco.AI.Persistence.Versioning;
using Umbraco.AI.Tests.Common.Fixtures;

namespace Umbraco.AI.Tests.Unit.Repositories;

[Collection("EfCoreEntityVersionTests")]
public class EfCoreAIEntityVersionRepositoryTests : IClassFixture<EfCoreTestFixture>
{
    private readonly EfCoreTestFixture _fixture;

    public EfCoreAIEntityVersionRepositoryTests(EfCoreTestFixture fixture)
    {
        _fixture = fixture;
    }

    private EfCoreAIEntityVersionRepository CreateRepository(UmbracoAIDbContext context)
    {
        var scopeProvider = new TestEfCoreScopeProvider(() => context);
        return new EfCoreAIEntityVersionRepository(scopeProvider);
    }

    private async Task ClearEntityVersionsAsync()
    {
        await using var context = _fixture.CreateContext();
        context.EntityVersions.RemoveRange(context.EntityVersions);
        await context.SaveChangesAsync();
    }

    #region DeleteExcessVersionsAsync

    [Fact]
    public async Task DeleteExcessVersionsAsync_WithNoVersions_ReturnsZero()
    {
        // Arrange & Act
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);
        var result = await repository.DeleteExcessVersionsAsync(maxVersionsPerEntity: 5);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteExcessVersionsAsync_WithVersionsBelowLimit_DeletesNothing()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        // Add 3 versions (below limit of 5) directly to context
        setupContext.EntityVersions.AddRange(
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 1, Snapshot = "snapshot1", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 2, Snapshot = "snapshot2", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 3, Snapshot = "snapshot3", DateCreated = DateTime.UtcNow }
        );
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var deleted = await CreateRepository(actContext).DeleteExcessVersionsAsync(maxVersionsPerEntity: 5);

        // Assert
        deleted.ShouldBe(0);
        await using var assertContext = _fixture.CreateContext();
        var count = await CreateRepository(assertContext).GetVersionCountByEntityAsync(entityId, "Profile");
        count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteExcessVersionsAsync_WithVersionsAboveLimit_DeletesOldestVersions()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        // Add 7 versions (above limit of 3)
        for (int i = 1; i <= 7; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = "Profile",
                Version = i,
                Snapshot = $"snapshot{i}",
                DateCreated = DateTime.UtcNow
            });
        }
        await setupContext.SaveChangesAsync();

        // Act - keep only 3 most recent versions
        await using var actContext = _fixture.CreateContext();
        var deleted = await CreateRepository(actContext).DeleteExcessVersionsAsync(maxVersionsPerEntity: 3);

        // Assert
        deleted.ShouldBe(4); // Should delete versions 1, 2, 3, 4

        await using var assertContext1 = _fixture.CreateContext();
        var remainingCount = await CreateRepository(assertContext1).GetVersionCountByEntityAsync(entityId, "Profile");
        remainingCount.ShouldBe(3);

        // Verify the correct versions remain (5, 6, 7)
        await using var assertContext2 = _fixture.CreateContext();
        var version5 = await CreateRepository(assertContext2).GetVersionAsync(entityId, "Profile", 5);
        await using var assertContext3 = _fixture.CreateContext();
        var version6 = await CreateRepository(assertContext3).GetVersionAsync(entityId, "Profile", 6);
        await using var assertContext4 = _fixture.CreateContext();
        var version7 = await CreateRepository(assertContext4).GetVersionAsync(entityId, "Profile", 7);
        version5.ShouldNotBeNull();
        version6.ShouldNotBeNull();
        version7.ShouldNotBeNull();

        // Verify old versions were deleted
        await using var assertContext5 = _fixture.CreateContext();
        var version1 = await CreateRepository(assertContext5).GetVersionAsync(entityId, "Profile", 1);
        await using var assertContext6 = _fixture.CreateContext();
        var version2 = await CreateRepository(assertContext6).GetVersionAsync(entityId, "Profile", 2);
        version1.ShouldBeNull();
        version2.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteExcessVersionsAsync_WithMultipleEntities_DeletesPerEntityIndependently()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();

        var entity1 = Guid.NewGuid();
        var entity2 = Guid.NewGuid();

        // Entity 1: 5 versions (2 should be deleted with limit 3)
        for (int i = 1; i <= 5; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entity1,
                EntityType = "Profile",
                Version = i,
                Snapshot = $"snapshot{i}",
                DateCreated = DateTime.UtcNow
            });
        }

        // Entity 2: 4 versions (1 should be deleted with limit 3)
        for (int i = 1; i <= 4; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entity2,
                EntityType = "Profile",
                Version = i,
                Snapshot = $"snapshot{i}",
                DateCreated = DateTime.UtcNow
            });
        }
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var deleted = await CreateRepository(actContext).DeleteExcessVersionsAsync(maxVersionsPerEntity: 3);

        // Assert
        deleted.ShouldBe(3); // 2 from entity1, 1 from entity2

        await using var assertContext1 = _fixture.CreateContext();
        var entity1Count = await CreateRepository(assertContext1).GetVersionCountByEntityAsync(entity1, "Profile");
        await using var assertContext2 = _fixture.CreateContext();
        var entity2Count = await CreateRepository(assertContext2).GetVersionCountByEntityAsync(entity2, "Profile");
        entity1Count.ShouldBe(3);
        entity2Count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteExcessVersionsAsync_WithDifferentEntityTypes_DeletesPerTypeIndependently()
    {
        // Arrange
        await ClearEntityVersionsAsync();
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        // Same EntityId but different EntityType should be treated separately
        // Profile versions
        for (int i = 1; i <= 5; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = "Profile",
                Version = i,
                Snapshot = $"profile{i}",
                DateCreated = DateTime.UtcNow
            });
        }

        // Connection versions (same EntityId, different type)
        for (int i = 1; i <= 4; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = "Connection",
                Version = i,
                Snapshot = $"connection{i}",
                DateCreated = DateTime.UtcNow
            });
        }
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var deleted = await CreateRepository(actContext).DeleteExcessVersionsAsync(maxVersionsPerEntity: 2);

        // Assert
        deleted.ShouldBe(5); // 3 from Profile, 2 from Connection

        await using var assertContext1 = _fixture.CreateContext();
        var profileCount = await CreateRepository(assertContext1).GetVersionCountByEntityAsync(entityId, "Profile");
        await using var assertContext2 = _fixture.CreateContext();
        var connectionCount = await CreateRepository(assertContext2).GetVersionCountByEntityAsync(entityId, "Connection");
        profileCount.ShouldBe(2);
        connectionCount.ShouldBe(2);
    }

    #endregion

    #region SaveVersionAsync & GetVersionAsync

    [Fact]
    public async Task SaveVersionAsync_WithValidData_SavesVersion()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        await using var actContext = _fixture.CreateContext();
        var repository = CreateRepository(actContext);
        await repository.SaveVersionAsync(
            entityId,
            "Profile",
            1,
            "test-snapshot",
            userId,
            "Initial creation");

        // Assert
        await using var assertContext = _fixture.CreateContext();
        var version = await CreateRepository(assertContext).GetVersionAsync(entityId, "Profile", 1);
        version.ShouldNotBeNull();
        version!.EntityId.ShouldBe(entityId);
        version.EntityType.ShouldBe("Profile");
        version.Version.ShouldBe(1);
        version.Snapshot.ShouldBe("test-snapshot");
        version.CreatedByUserId.ShouldBe(userId);
        version.ChangeDescription.ShouldBe("Initial creation");
    }

    [Fact]
    public async Task GetVersionAsync_WithNonExistentVersion_ReturnsNull()
    {
        // Arrange & Act
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);
        var entityId = Guid.NewGuid();
        var version = await repository.GetVersionAsync(entityId, "Profile", 999);

        // Assert
        version.ShouldBeNull();
    }

    #endregion

    #region GetVersionHistoryAsync

    [Fact]
    public async Task GetVersionHistoryAsync_ReturnsVersionsInDescendingOrder()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        // Add versions out of order
        setupContext.EntityVersions.AddRange(
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 2, Snapshot = "snapshot2", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 1, Snapshot = "snapshot1", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 3, Snapshot = "snapshot3", DateCreated = DateTime.UtcNow }
        );
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var history = await CreateRepository(actContext).GetVersionHistoryAsync(entityId, "Profile", skip: 0, take: 10);

        // Assert
        var versions = history.ToList();
        versions.Count.ShouldBe(3);
        versions[0].Version.ShouldBe(3);
        versions[1].Version.ShouldBe(2);
        versions[2].Version.ShouldBe(1);
    }

    [Fact]
    public async Task GetVersionHistoryAsync_WithPagination_ReturnsCorrectSubset()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            setupContext.EntityVersions.Add(new AIEntityVersionEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = "Profile",
                Version = i,
                Snapshot = $"snapshot{i}",
                DateCreated = DateTime.UtcNow
            });
        }
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var history = await CreateRepository(actContext).GetVersionHistoryAsync(entityId, "Profile", skip: 1, take: 2);

        // Assert
        var versions = history.ToList();
        versions.Count.ShouldBe(2);
        versions[0].Version.ShouldBe(4); // Second newest
        versions[1].Version.ShouldBe(3); // Third newest
    }

    #endregion

    #region GetVersionCountByEntityAsync

    [Fact]
    public async Task GetVersionCountByEntityAsync_WithNoVersions_ReturnsZero()
    {
        // Arrange & Act
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);
        var count = await repository.GetVersionCountByEntityAsync(Guid.NewGuid(), "Profile");

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetVersionCountByEntityAsync_WithMultipleVersions_ReturnsCorrectCount()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        setupContext.EntityVersions.AddRange(
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 1, Snapshot = "snapshot1", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 2, Snapshot = "snapshot2", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 3, Snapshot = "snapshot3", DateCreated = DateTime.UtcNow }
        );
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var count = await CreateRepository(actContext).GetVersionCountByEntityAsync(entityId, "Profile");

        // Assert
        count.ShouldBe(3);
    }

    #endregion

    #region DeleteVersionsAsync

    [Fact]
    public async Task DeleteVersionsAsync_RemovesAllVersionsForEntity()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        setupContext.EntityVersions.AddRange(
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 1, Snapshot = "snapshot1", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entityId, EntityType = "Profile", Version = 2, Snapshot = "snapshot2", DateCreated = DateTime.UtcNow }
        );
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        await CreateRepository(actContext).DeleteVersionsAsync(entityId, "Profile");

        // Assert
        await using var assertContext = _fixture.CreateContext();
        var count = await CreateRepository(assertContext).GetVersionCountByEntityAsync(entityId, "Profile");
        count.ShouldBe(0);
    }

    #endregion

    #region DeleteVersionsOlderThanAsync

    [Fact]
    public async Task DeleteVersionsOlderThanAsync_DeletesOldVersions()
    {
        // Arrange
        await using var setupContext = _fixture.CreateContext();
        var entityId = Guid.NewGuid();

        // Manually insert old version with past DateCreated
        var oldDate = DateTime.UtcNow.AddDays(-10);
        setupContext.EntityVersions.Add(new AIEntityVersionEntity
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = "Profile",
            Version = 1,
            Snapshot = "old-snapshot",
            DateCreated = oldDate
        });
        setupContext.EntityVersions.Add(new AIEntityVersionEntity
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = "Profile",
            Version = 2,
            Snapshot = "recent-snapshot",
            DateCreated = DateTime.UtcNow
        });
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var threshold = DateTime.UtcNow.AddDays(-5);
        var deleted = await CreateRepository(actContext).DeleteVersionsOlderThanAsync(threshold);

        // Assert
        await using var assertContext = _fixture.CreateContext();
        deleted.ShouldBe(1);

        var count = await CreateRepository(assertContext).GetVersionCountByEntityAsync(entityId, "Profile");
        count.ShouldBe(1); // Only recent version remains
    }

    #endregion

    #region GetVersionCountAsync

    [Fact]
    public async Task GetVersionCountAsync_ReturnsAllVersionsCount()
    {
        // Arrange
        await ClearEntityVersionsAsync();
        await using var setupContext = _fixture.CreateContext();

        var entity1 = Guid.NewGuid();
        var entity2 = Guid.NewGuid();

        setupContext.EntityVersions.AddRange(
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entity1, EntityType = "Profile", Version = 1, Snapshot = "snapshot1", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entity1, EntityType = "Profile", Version = 2, Snapshot = "snapshot2", DateCreated = DateTime.UtcNow },
            new AIEntityVersionEntity { Id = Guid.NewGuid(), EntityId = entity2, EntityType = "Connection", Version = 1, Snapshot = "snapshot3", DateCreated = DateTime.UtcNow }
        );
        await setupContext.SaveChangesAsync();

        // Act
        await using var actContext = _fixture.CreateContext();
        var totalCount = await CreateRepository(actContext).GetVersionCountAsync();

        // Assert
        totalCount.ShouldBe(3);
    }

    #endregion
}

/// <summary>
/// Collection definition to prevent parallel test execution for entity version repository tests.
/// </summary>
[CollectionDefinition("EfCoreEntityVersionTests", DisableParallelization = true)]
public class EfCoreEntityVersionTestsCollection
{
}
