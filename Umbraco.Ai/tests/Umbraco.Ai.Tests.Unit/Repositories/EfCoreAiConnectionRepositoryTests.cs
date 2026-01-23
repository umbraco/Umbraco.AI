using System.Text.Json;
using Umbraco.Ai.Core;
using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Persistence;
using Umbraco.Ai.Persistence.Connections;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fixtures;

namespace Umbraco.Ai.Tests.Unit.Repositories;

public class EfCoreAiConnectionRepositoryTests : IClassFixture<EfCoreTestFixture>
{
    private readonly EfCoreTestFixture _fixture;
    private readonly Mock<IAiConnectionFactory> _connectionFactoryMock;

    public EfCoreAiConnectionRepositoryTests(EfCoreTestFixture fixture)
    {
        _fixture = fixture;
        _connectionFactoryMock = new Mock<IAiConnectionFactory>();

        // Setup mock to pass through values without encryption for testing
        _connectionFactoryMock
            .Setup(f => f.BuildDomain(It.IsAny<AiConnectionEntity>()))
            .Returns<AiConnectionEntity>(entity => new AiConnection
            {
                Id = entity.Id,
                Alias = entity.Alias,
                Name = entity.Name,
                ProviderId = entity.ProviderId,
                Settings = string.IsNullOrEmpty(entity.Settings)
                    ? null
                    : JsonSerializer.Deserialize<JsonElement>(entity.Settings, Constants.DefaultJsonSerializerOptions),
                IsActive = entity.IsActive,
                DateCreated = entity.DateCreated,
                DateModified = entity.DateModified,
                CreatedByUserId = entity.CreatedByUserId,
                ModifiedByUserId = entity.ModifiedByUserId
            });

        _connectionFactoryMock
            .Setup(f => f.BuildEntity(It.IsAny<AiConnection>()))
            .Returns<AiConnection>(conn => new AiConnectionEntity
            {
                Id = conn.Id,
                Alias = conn.Alias,
                Name = conn.Name,
                ProviderId = conn.ProviderId,
                Settings = conn.Settings == null
                    ? null
                    : JsonSerializer.Serialize(conn.Settings, Constants.DefaultJsonSerializerOptions),
                IsActive = conn.IsActive,
                DateCreated = conn.DateCreated,
                DateModified = conn.DateModified,
                CreatedByUserId = conn.CreatedByUserId,
                ModifiedByUserId = conn.ModifiedByUserId
            });

        _connectionFactoryMock
            .Setup(f => f.UpdateEntity(It.IsAny<AiConnectionEntity>(), It.IsAny<AiConnection>()))
            .Callback<AiConnectionEntity, AiConnection>((entity, conn) =>
            {
                entity.Alias = conn.Alias;
                entity.Name = conn.Name;
                entity.ProviderId = conn.ProviderId;
                entity.Settings = conn.Settings == null
                    ? null
                    : JsonSerializer.Serialize(conn.Settings, Constants.DefaultJsonSerializerOptions);
                entity.IsActive = conn.IsActive;
                entity.DateModified = conn.DateModified;
                entity.ModifiedByUserId = conn.ModifiedByUserId;
            });
    }

    private EfCoreAiConnectionRepository CreateRepository(UmbracoAiDbContext context)
    {
        var scopeProvider = new TestEfCoreScopeProvider(() => context);
        return new EfCoreAiConnectionRepository(scopeProvider, _connectionFactoryMock.Object);
    }

    #region GetAsync

    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.GetAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenExists_ReturnsConnection()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = connectionId,
            Alias = $"test-connection-{connectionId:N}",
            Name = "Test Connection",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetAsync(connectionId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(connectionId);
        result.Name.ShouldBe("Test Connection");
        result.ProviderId.ShouldBe("openai");
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange - use fresh context
        await using var context = _fixture.CreateContext();
        // Clear any existing data
        context.Connections.RemoveRange(context.Connections);
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllConnections()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        // Clear existing data
        context.Connections.RemoveRange(context.Connections);
        var conn1Id = Guid.NewGuid();
        var conn2Id = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = conn1Id,
            Alias = $"connection-1-{conn1Id:N}",
            Name = "Connection 1",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        context.Connections.Add(new AiConnectionEntity
        {
            Id = conn2Id,
            Alias = $"connection-2-{conn2Id:N}",
            Name = "Connection 2",
            ProviderId = "azure",
            IsActive = true,
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

    #region GetByProviderAsync

    [Fact]
    public async Task GetByProviderAsync_FiltersCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        // Clear existing
        context.Connections.RemoveRange(context.Connections);
        var openaiId = Guid.NewGuid();
        var azureId = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = openaiId,
            Alias = $"openai-connection-{openaiId:N}",
            Name = "OpenAI Connection",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        context.Connections.Add(new AiConnectionEntity
        {
            Id = azureId,
            Alias = $"azure-connection-{azureId:N}",
            Name = "Azure Connection",
            ProviderId = "azure",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByProviderAsync("openai");

        // Assert
        result.Count().ShouldBe(1);
        result.First().ProviderId.ShouldBe("openai");
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_NewConnection_InsertsEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);
        var connection = new AiConnectionBuilder()
            .WithName("New Connection")
            .WithProviderId("openai")
            .Build();

        // Act
        var result = await repository.SaveAsync(connection);

        // Assert
        result.ShouldNotBeNull();

        // Verify it was persisted
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Connections.FindAsync(connection.Id);
        saved.ShouldNotBeNull();
        saved!.Name.ShouldBe("New Connection");
    }

    [Fact]
    public async Task SaveAsync_ExistingConnection_UpdatesEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = connectionId,
            Alias = "original-connection",
            Name = "Original Name",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Create repository with fresh context
        var repository = CreateRepository(_fixture.CreateContext());
        var updated = new AiConnection
        {
            Id = connectionId,
            Alias = "updated-connection",
            Name = "Updated Name",
            ProviderId = "openai",
            IsActive = false,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Act
        await repository.SaveAsync(updated);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Connections.FindAsync(connectionId);
        saved.ShouldNotBeNull();
        saved!.Name.ShouldBe("Updated Name");
        saved.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithSettings_SerializesAsJson()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);
        var connection = new AiConnectionBuilder()
            .WithName("Connection With Settings")
            .WithProviderId("openai")
            .WithSettings(new { ApiKey = "test-key", Endpoint = "https://api.openai.com" })
            .Build();

        // Act
        await repository.SaveAsync(connection);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Connections.FindAsync(connection.Id);
        saved.ShouldNotBeNull();
        saved!.Settings.ShouldNotBeNullOrEmpty();
        saved.Settings.ShouldContain("ApiKey");
        saved.Settings.ShouldContain("test-key");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = connectionId,
            Alias = $"to-delete-{connectionId:N}",
            Name = "To Delete",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.DeleteAsync(connectionId);

        // Assert
        result.ShouldBeTrue();

        await using var verifyContext = _fixture.CreateContext();
        var deleted = await verifyContext.Connections.FindAsync(connectionId);
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

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = Guid.NewGuid();
        context.Connections.Add(new AiConnectionEntity
        {
            Id = connectionId,
            Alias = $"existing-{connectionId:N}",
            Name = "Existing",
            ProviderId = "openai",
            IsActive = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.ExistsAsync(connectionId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = CreateRepository(context);

        // Act
        var result = await repository.ExistsAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
