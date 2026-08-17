using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Strings;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.FileStore;

/// <summary>
/// Files are addressed by a thread id and file id that both arrive from a client-supplied route, so
/// the store is the single place that decides whether the caller may read the bytes. These tests pin
/// that decision, including that it fails closed.
/// </summary>
public class AIFileStoreOwnershipTests
{
    private const string ThreadId = "thread-1";
    private const string FileId = "file-abc";
    private static readonly Guid OwnerKey = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserKey = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly byte[] FileBytes = [1, 2, 3, 4];

    [Fact]
    public async Task ResolveAsync_WhenOwnedByCurrentUser_ReturnsFile()
    {
        // Arrange
        var store = CreateStore(storedOwnerKey: OwnerKey.ToString(), currentUserKey: OwnerKey);

        // Act
        var result = await store.ResolveAsync(ThreadId, FileId);

        // Assert
        result.ShouldNotBeNull();
        result!.Data.ShouldBe(FileBytes);
        result.MimeType.ShouldBe("image/png");
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnedByAnotherUser_ReturnsNull()
    {
        // Arrange
        var store = CreateStore(storedOwnerKey: OwnerKey.ToString(), currentUserKey: OtherUserKey);

        // Act
        var result = await store.ResolveAsync(ThreadId, FileId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenNoOwnerRecorded_ReturnsNull()
    {
        // Arrange
        // Files written before ownership was recorded. Fail closed: they age out via retention rather
        // than staying readable by whoever holds the URL.
        var store = CreateStore(storedOwnerKey: null, currentUserKey: OwnerKey);

        // Act
        var result = await store.ResolveAsync(ThreadId, FileId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WithNoCurrentUser_ReturnsNull()
    {
        // Arrange
        var store = CreateStore(storedOwnerKey: OwnerKey.ToString(), currentUserKey: null);

        // Act
        var result = await store.ResolveAsync(ThreadId, FileId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenPathEscapesTheStoreRoot_ReturnsNullRatherThanThrowing()
    {
        // Arrange
        // The ids are client-supplied. The file system refuses to resolve a path outside its root by
        // throwing; that must surface as "not found", not as a server error.
        var fileSystem = new Mock<IFileSystem>();
        fileSystem
            .Setup(x => x.FileExists(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("outside this filesystem's root"));

        var store = CreateStore(fileSystem, currentUserKey: OwnerKey);

        // Act
        var result = await store.ResolveAsync("../../etc", "passwd");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task StoreAsync_RecordsTheCurrentUserAsOwner()
    {
        // Arrange
        var written = new Dictionary<string, byte[]>();
        var fileSystem = new Mock<IFileSystem>();
        fileSystem
            .Setup(x => x.AddFile(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
            .Callback<string, Stream, bool>((path, stream, _) =>
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                written[path] = ms.ToArray();
            });

        var store = CreateStore(fileSystem, currentUserKey: OwnerKey);

        // Act
        var fileId = await store.StoreAsync(ThreadId, FileBytes, "image/png", "shot.png");

        // Assert
        var metadataJson = written.Single(kvp => kvp.Key.EndsWith($"{fileId}.json", StringComparison.Ordinal)).Value;
        using var document = JsonDocument.Parse(metadataJson);
        document.RootElement.GetProperty("OwnerKey").GetString().ShouldBe(OwnerKey.ToString());
    }

    private static AIFileStore CreateStore(string? storedOwnerKey, Guid? currentUserKey)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            MimeType = "image/png",
            Filename = "shot.png",
            OwnerKey = storedOwnerKey,
        });

        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        fileSystem
            .Setup(x => x.OpenFile(It.Is<string>(p => p.EndsWith(".json", StringComparison.Ordinal))))
            .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(metadata)));
        fileSystem
            .Setup(x => x.OpenFile(It.Is<string>(p => p.EndsWith(".bin", StringComparison.Ordinal))))
            .Returns(() => new MemoryStream(FileBytes));

        return CreateStore(fileSystem, currentUserKey);
    }

    private static AIFileStore CreateStore(Mock<IFileSystem> fileSystem, Guid? currentUserKey)
    {
        // The five-arg constructor resolves a scope provider from the static service locator, which
        // is not set up in unit tests. Only FileSystem is exercised here.
        var mediaFileManager = new MediaFileManager(
            fileSystem.Object,
            Mock.Of<IMediaPathScheme>(),
            NullLogger<MediaFileManager>.Instance,
            Mock.Of<IShortStringHelper>(),
            Mock.Of<IServiceProvider>(),
            new Lazy<ICoreScopeProvider>(() => Mock.Of<ICoreScopeProvider>()));

        return new AIFileStore(
            mediaFileManager,
            NullLogger<AIFileStore>.Instance,
            CreateSecurityAccessor(currentUserKey));
    }

    private static IBackOfficeSecurityAccessor CreateSecurityAccessor(Guid? currentUserKey)
    {
        var security = new Mock<IBackOfficeSecurity>();

        if (currentUserKey is not null)
        {
            var user = new Mock<IUser>();
            user.Setup(u => u.Key).Returns(currentUserKey.Value);
            security.Setup(s => s.CurrentUser).Returns(user.Object);
        }

        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(a => a.BackOfficeSecurity).Returns(security.Object);
        return accessor.Object;
    }
}
