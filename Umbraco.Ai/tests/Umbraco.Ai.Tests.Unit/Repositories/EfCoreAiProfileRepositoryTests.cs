using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Persistence;
using Umbraco.Ai.Persistence.Connections;
using Umbraco.Ai.Persistence.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Tests.Common.Fixtures;

namespace Umbraco.Ai.Tests.Unit.Repositories;

public class EfCoreAiProfileRepositoryTests : IClassFixture<EfCoreTestFixture>
{
    private readonly EfCoreTestFixture _fixture;

    public EfCoreAiProfileRepositoryTests(EfCoreTestFixture fixture)
    {
        _fixture = fixture;
    }

    private EfCoreAiProfileRepository CreateRepository(UmbracoAiDbContext context)
    {
        var scopeProvider = new TestEfCoreScopeProvider(() => context);
        return new EfCoreAiProfileRepository(scopeProvider);
    }

    private async Task<Guid> EnsureConnectionExists(UmbracoAiDbContext context)
    {
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
        return connectionId;
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
    public async Task GetByIdAsync_WhenExists_ReturnsProfile()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        var profileId = Guid.NewGuid();
        context.Profiles.Add(new AiProfileEntity
        {
            Id = profileId,
            Alias = "test-profile",
            Name = "Test Profile",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(profileId);

        // Assert
        result.ShouldNotBeNull();
        result!.Id.ShouldBe(profileId);
        result.Alias.ShouldBe("test-profile");
        result.Name.ShouldBe("Test Profile");
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
    public async Task GetByAliasAsync_WhenExists_ReturnsProfile()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "my-profile",
            Name = "My Profile",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByAliasAsync("my-profile");

        // Assert
        result.ShouldNotBeNull();
        result!.Alias.ShouldBe("my-profile");
    }

    [Fact]
    public async Task GetByAliasAsync_IsCaseInsensitive()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "CaseSensitive",
            Name = "Case Test",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
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
    public async Task GetAllAsync_ReturnsAllProfiles()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        // Clear existing profiles
        context.Profiles.RemoveRange(context.Profiles);
        var connectionId = await EnsureConnectionExists(context);
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "profile-1",
            Name = "Profile 1",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "profile-2",
            Name = "Profile 2",
            Capability = (int)AiCapability.Embedding,
            ProviderId = "openai",
            ModelId = "text-embedding-ada",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Count().ShouldBe(2);
    }

    #endregion

    #region GetByCapability

    [Fact]
    public async Task GetByCapability_FiltersCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        context.Profiles.RemoveRange(context.Profiles);
        var connectionId = await EnsureConnectionExists(context);
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "chat-profile",
            Name = "Chat Profile",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        context.Profiles.Add(new AiProfileEntity
        {
            Id = Guid.NewGuid(),
            Alias = "embedding-profile",
            Name = "Embedding Profile",
            Capability = (int)AiCapability.Embedding,
            ProviderId = "openai",
            ModelId = "text-embedding-ada",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByCapability(AiCapability.Chat);

        // Assert
        result.Count().ShouldBe(1);
        result.First().Capability.ShouldBe(AiCapability.Chat);
    }

    #endregion

    #region SaveAsync

    [Fact]
    public async Task SaveAsync_NewProfile_InsertsEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);

        var repository = CreateRepository(_fixture.CreateContext());
        var profile = new AiProfileBuilder()
            .WithAlias("new-profile")
            .WithName("New Profile")
            .WithCapability(AiCapability.Chat)
            .WithConnectionId(connectionId)
            .Build();

        // Act
        var result = await repository.SaveAsync(profile);

        // Assert
        result.ShouldNotBeNull();

        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Profiles.FindAsync(profile.Id);
        saved.ShouldNotBeNull();
        saved!.Alias.ShouldBe("new-profile");
    }

    [Fact]
    public async Task SaveAsync_ExistingProfile_UpdatesEntity()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        var profileId = Guid.NewGuid();
        context.Profiles.Add(new AiProfileEntity
        {
            Id = profileId,
            Alias = "original-alias",
            Name = "Original Name",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());
        var updated = new AiProfile
        {
            Id = profileId,
            Alias = "updated-alias",
            Name = "Updated Name",
            Capability = AiCapability.Chat,
            Model = new AiModelRef("openai", "gpt-4-turbo"),
            ConnectionId = connectionId
        };

        // Act
        await repository.SaveAsync(updated);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Profiles.FindAsync(profileId);
        saved.ShouldNotBeNull();
        saved!.Alias.ShouldBe("updated-alias");
        saved.Name.ShouldBe("Updated Name");
        saved.ModelId.ShouldBe("gpt-4-turbo");
    }

    [Fact]
    public async Task SaveAsync_WithTags_SerializesAsJson()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);

        var repository = CreateRepository(_fixture.CreateContext());
        var profile = new AiProfile
        {
            Id = Guid.NewGuid(),
            Alias = "tagged-profile",
            Name = "Tagged Profile",
            Capability = AiCapability.Chat,
            Model = new AiModelRef("openai", "gpt-4"),
            ConnectionId = connectionId,
            Tags = new List<string> { "tag1", "tag2", "tag3" }
        };

        // Act
        await repository.SaveAsync(profile);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Profiles.FindAsync(profile.Id);
        saved.ShouldNotBeNull();
        saved!.Tags.ShouldNotBeNullOrEmpty();
        saved.Tags.ShouldContain("tag1");
        saved.Tags.ShouldContain("tag2");
    }

    [Fact]
    public async Task SaveAsync_WithOptionalFields_PersistsCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);

        var repository = CreateRepository(_fixture.CreateContext());
        var profile = new AiProfile
        {
            Id = Guid.NewGuid(),
            Alias = "full-profile",
            Name = "Full Profile",
            Capability = AiCapability.Chat,
            Model = new AiModelRef("openai", "gpt-4"),
            ConnectionId = connectionId,
            Settings = new AiChatProfileSettings
            {
                Temperature = 0.7f,
                MaxTokens = 2000,
                SystemPromptTemplate = "You are a helpful assistant."
            }
        };

        // Act
        await repository.SaveAsync(profile);

        // Assert
        await using var verifyContext = _fixture.CreateContext();
        var saved = await verifyContext.Profiles.FindAsync(profile.Id);
        saved.ShouldNotBeNull();
        saved!.Settings.ShouldNotBeNull();
        saved.Settings.ShouldContain("0.7");
        saved.Settings.ShouldContain("2000");
        saved.Settings.ShouldContain("You are a helpful assistant.");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        var profileId = Guid.NewGuid();
        context.Profiles.Add(new AiProfileEntity
        {
            Id = profileId,
            Alias = "to-delete",
            Name = "To Delete",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.DeleteAsync(profileId);

        // Assert
        result.ShouldBeTrue();

        await using var verifyContext = _fixture.CreateContext();
        var deleted = await verifyContext.Profiles.FindAsync(profileId);
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

    #region Model Mapping

    [Fact]
    public async Task GetByIdAsync_MapsModelRefCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        var profileId = Guid.NewGuid();
        context.Profiles.Add(new AiProfileEntity
        {
            Id = profileId,
            Alias = "model-test",
            Name = "Model Test",
            Capability = (int)AiCapability.Chat,
            ProviderId = "openai",
            ModelId = "gpt-4-turbo",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(profileId);

        // Assert
        result.ShouldNotBeNull();
        result!.Model.ProviderId.ShouldBe("openai");
        result.Model.ModelId.ShouldBe("gpt-4-turbo");
    }

    [Fact]
    public async Task GetByIdAsync_MapsCapabilityCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var connectionId = await EnsureConnectionExists(context);
        var profileId = Guid.NewGuid();
        context.Profiles.Add(new AiProfileEntity
        {
            Id = profileId,
            Alias = "embedding-test",
            Name = "Embedding Test",
            Capability = (int)AiCapability.Embedding,
            ProviderId = "openai",
            ModelId = "text-embedding-ada",
            ConnectionId = connectionId
        });
        await context.SaveChangesAsync();

        var repository = CreateRepository(_fixture.CreateContext());

        // Act
        var result = await repository.GetByIdAsync(profileId);

        // Assert
        result.ShouldNotBeNull();
        result!.Capability.ShouldBe(AiCapability.Embedding);
    }

    #endregion
}
