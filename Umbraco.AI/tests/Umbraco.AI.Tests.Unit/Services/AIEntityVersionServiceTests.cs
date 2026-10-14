using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Tests.Common.Builders;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIEntityVersionServiceTests
{
    private readonly InMemoryAIEntityVersionRepository _repository = new();
    private readonly AIEntityVersionService _service;

    public AIEntityVersionServiceTests()
    {
        var adapterMock = new Mock<IAIVersionableEntityAdapter>();
        adapterMock.SetupGet(x => x.EntityTypeName).Returns("Profile");
        adapterMock.SetupGet(x => x.EntityType).Returns(typeof(AIProfile));
        adapterMock.Setup(x => x.CreateSnapshot(It.IsAny<object>())).Returns("snapshot");

        _service = new AIEntityVersionService(
            _repository,
            new AIVersionableEntityAdapterCollection(() => [adapterMock.Object]),
            Mock.Of<IOptionsMonitor<AIVersionCleanupPolicy>>());
    }

    [Fact]
    public async Task SaveVersionAsync_WhenAnotherUserUpdates_RecordsAuthorAndArchiverSeparately()
    {
        // Arrange - user A made version 1; user B's save is now archiving it
        var authorId = Guid.NewGuid();
        var archiverId = Guid.NewGuid();
        var authoredAt = new DateTime(2026, 3, 1, 9, 30, 0, DateTimeKind.Utc);
        var profile = new AIProfileBuilder().Build();
        profile.CreatedByUserId = authorId;
        profile.ModifiedByUserId = authorId;
        profile.DateModified = authoredAt;
        var before = DateTime.UtcNow;

        // Act
        await _service.SaveVersionAsync(profile, archiverId);

        // Assert
        var version = await _repository.GetVersionAsync(profile.Id, "Profile", profile.Version);
        version.ShouldNotBeNull();
        version!.AuthoredByUserId.ShouldBe(authorId);
        version.DateAuthored.ShouldBe(authoredAt);
        version.CreatedByUserId.ShouldBe(archiverId);
        version.DateCreated.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public async Task SaveVersionAsync_WhenEntityHasNoModifyingUser_LeavesAuthorUnknown()
    {
        // Arrange - crediting the creator or the archiver here would misattribute the version
        var profile = new AIProfileBuilder().Build();
        profile.CreatedByUserId = Guid.NewGuid();
        profile.ModifiedByUserId = null;

        // Act
        await _service.SaveVersionAsync(profile, Guid.NewGuid());

        // Assert
        var version = await _repository.GetVersionAsync(profile.Id, "Profile", profile.Version);
        version.ShouldNotBeNull();
        version!.AuthoredByUserId.ShouldBeNull();
    }
}
