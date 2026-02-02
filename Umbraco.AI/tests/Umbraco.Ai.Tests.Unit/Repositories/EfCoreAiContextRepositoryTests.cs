using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Persistence;
using Umbraco.Ai.Persistence.Context;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fixtures;

namespace Umbraco.Ai.Tests.Unit.Repositories;

public class EfCoreAiContextRepositoryTests : IClassFixture<EfCoreTestFixture>
{
    private readonly EfCoreTestFixture _fixture;

    public EfCoreAiContextRepositoryTests(EfCoreTestFixture fixture)
    {
        _fixture = fixture;
    }

    private EfCoreAiContextRepository CreateRepository(UmbracoAiDbContext context)
    {
        var scopeProvider = new TestEfCoreScopeProvider(() => context);
        return new EfCoreAiContextRepository(scopeProvider);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsContext()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "test-context",
            Name = "Test Context",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(contextId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(contextId);
        result.Alias.ShouldBe("test-context");
        result.Name.ShouldBe("Test Context");
    }

    [Fact]
    public async Task GetByIdAsync_IncludesResources()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "context-with-resources",
            Name = "Context With Resources",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            Resources = new List<AiContextResourceEntity>
            {
                new()
                {
                    Id = resourceId,
                    ContextId = contextId,
                    ResourceTypeId = "brand-voice",
                    Name = "Brand Voice",
                    Data = "{}",
                    SortOrder = 0,
                    InjectionMode = 0
                }
            }
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(contextId);

        // Assert
        result.ShouldNotBeNull();
        result!.Resources.ShouldNotBeEmpty();
        result.Resources.Count.ShouldBe(1);
        result.Resources[0].Name.ShouldBe("Brand Voice");
    }

    #endregion

    #region GetByAliasAsync

    [Fact]
    public async Task GetByAliasAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetByAliasAsync("non-existent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByAliasAsync_WhenExists_ReturnsContext()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "my-context",
            Name = "My Context",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByAliasAsync("my-context");

        // Assert
        result.ShouldNotBeNull();
        result!.Alias.ShouldBe("my-context");
    }

    [Fact]
    public async Task GetByAliasAsync_IsCaseInsensitive()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "CaseSensitive",
            Name = "Case Test",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByAliasAsync("casesensitive");

        // Assert
        result.ShouldNotBeNull();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllContexts()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        // Clear existing contexts
        context.Contexts.RemoveRange(context.Contexts);
        await context.SaveChangesAsync();

        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "context-1",
            Name = "Context 1",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "context-2",
            Name = "Context 2",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Count().ShouldBe(2);
    }

    #endregion

    #region GetPagedAsync

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        context.Contexts.RemoveRange(context.Contexts);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Contexts.Add(new AiContextEntity
            {
                Id = Guid.NewGuid(),
                Alias = $"paged-context-{i}",
                Name = $"Paged Context {i}",
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var (items, total) = await repository.GetPagedAsync(skip: 0, take: 3);

        // Assert
        items.Count().ShouldBe(3);
        total.ShouldBe(5);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilter_FiltersResults()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        context.Contexts.RemoveRange(context.Contexts);
        await context.SaveChangesAsync();

        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "brand-context",
            Name = "Brand Context",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        context.Contexts.Add(new AiContextEntity
        {
            Id = Guid.NewGuid(),
            Alias = "other-context",
            Name = "Other Context",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var (items, total) = await repository.GetPagedAsync(filter: "brand");

        // Assert
        items.Count().ShouldBe(1);
        total.ShouldBe(1);
        items.First().Alias.ShouldBe("brand-context");
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_NewContext_InsertsEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(_fixture.CreateContext());
        var aiContext = new AiContextBuilder()
            .WithAlias("new-context")
            .WithName("New Context")
            .Build();

        // Act
        var result = await repository.SaveAsync(aiContext);

        // Assert
        result.ShouldNotBeNull();

        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Contexts.FindAsync(aiContext.Id);
        saved.ShouldNotBeNull();
        saved!.Alias.ShouldBe("new-context");
    }

    [Fact]
    public async Task SaveAsync_ExistingContext_UpdatesEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "original-alias",
            Name = "Original Name",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());
        var updated = new AiContext
        {
            Id = contextId,
            Alias = "updated-alias",
            Name = "Updated Name"
        };

        // Act
        await repository.SaveAsync(updated);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Contexts.FindAsync(contextId);
        saved.ShouldNotBeNull();
        saved!.Alias.ShouldBe("updated-alias");
        saved.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task SaveAsync_WithResources_SavesResources()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(_fixture.CreateContext());

        var resource = new AiContextResourceBuilder()
            .WithName("Brand Voice")
            .AsBrandVoice()
            .Build();

        var aiContext = new AiContextBuilder()
            .WithAlias("context-with-resources")
            .WithName("Context with Resources")
            .WithResources(resource)
            .Build();

        // Act
        var result = await repository.SaveAsync(aiContext);

        // Assert
        result.ShouldNotBeNull();

        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Contexts
            .Include(c => c.Resources)
            .FirstOrDefaultAsync(c => c.Id == aiContext.Id);
        saved.ShouldNotBeNull();
        saved!.Resources.Count.ShouldBe(1);
        saved.Resources.First().Name.ShouldBe("Brand Voice");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "to-delete",
            Name = "To Delete",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.DeleteAsync(contextId);

        // Assert
        result.ShouldBeTrue();

        await using var verifyContext = _fixture.CreateContext();
        var deleted = await verifyContext.Contexts.FindAsync(contextId);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.DeleteAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_CascadesDeleteToResources()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "context-with-resource",
            Name = "Context with Resource",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            Resources = new List<AiContextResourceEntity>
            {
                new()
                {
                    Id = resourceId,
                    ContextId = contextId,
                    ResourceTypeId = "text",
                    Name = "Text Resource",
                    Data = "{}",
                    SortOrder = 0,
                    InjectionMode = 0
                }
            }
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.DeleteAsync(contextId);

        // Assert
        result.ShouldBeTrue();

        await using var verifyContext = _fixture.CreateContext();
        var deletedResource = await verifyContext.ContextResources.FindAsync(resourceId);
        deletedResource.ShouldBeNull();
    }

    #endregion

    #region Resource InjectionMode Mapping

    [Fact]
    public async Task GetByIdAsync_MapsInjectionModeCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var contextId = Guid.NewGuid();
        context.Contexts.Add(new AiContextEntity
        {
            Id = contextId,
            Alias = "injection-test",
            Name = "Injection Test",
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            Resources = new List<AiContextResourceEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ContextId = contextId,
                    ResourceTypeId = "text",
                    Name = "Always Resource",
                    Data = "{}",
                    SortOrder = 0,
                    InjectionMode = 0 // Always
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ContextId = contextId,
                    ResourceTypeId = "brand-voice",
                    Name = "OnDemand Resource",
                    Data = "{}",
                    SortOrder = 1,
                    InjectionMode = 1 // OnDemand
                }
            }
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(contextId);

        // Assert
        result.ShouldNotBeNull();
        result!.Resources.Count.ShouldBe(2);

        var alwaysResource = result.Resources.First(r => r.Name == "Always Resource");
        alwaysResource.InjectionMode.ShouldBe(AiContextResourceInjectionMode.Always);

        var onDemandResource = result.Resources.First(r => r.Name == "OnDemand Resource");
        onDemandResource.InjectionMode.ShouldBe(AiContextResourceInjectionMode.OnDemand);
    }

    #endregion
}
