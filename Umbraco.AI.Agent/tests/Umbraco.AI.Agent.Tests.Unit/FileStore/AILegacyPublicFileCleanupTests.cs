using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Strings;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.FileStore;

/// <summary>
/// Repointing the store at a private directory does nothing about uploads earlier versions already
/// wrote into the publicly served media directory. Those files stay readable, and the retention job
/// only ever looks at the store's own file system, so nothing would ever remove them. Without this
/// cleanup the upgrade would turn a 24-hour exposure into a permanent one.
/// </summary>
public class AILegacyPublicFileCleanupTests
{
    [Fact]
    public void DeleteLegacyFiles_WhenLegacyDirectoryExists_DeletesItRecursively()
    {
        // Arrange
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);

        var cleanup = CreateCleanup(fileSystem);

        // Act
        var deleted = cleanup.DeleteLegacyFiles();

        // Assert
        deleted.ShouldBeTrue();
        fileSystem.Verify(x => x.DeleteDirectory("agui-files", true), Times.Once);
    }

    [Fact]
    public void DeleteLegacyFiles_WhenNothingLeftBehind_DoesNothing()
    {
        // Arrange
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(false);

        var cleanup = CreateCleanup(fileSystem);

        // Act
        var deleted = cleanup.DeleteLegacyFiles();

        // Assert
        // Runs on every pass of the retention job, so the common case must be a cheap no-op.
        deleted.ShouldBeFalse();
        fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void DeleteLegacyFiles_WhenDeletionFails_DoesNotThrow()
    {
        // Arrange
        // A read-only or externally-managed media file system must not take down the retention job or
        // startup. The failure is logged so it can be cleared by hand, and retried next pass.
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);
        fileSystem
            .Setup(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new UnauthorizedAccessException("read-only"));

        var cleanup = CreateCleanup(fileSystem);

        // Act
        var deleted = Should.NotThrow(() => cleanup.DeleteLegacyFiles());

        // Assert
        deleted.ShouldBeFalse();
    }

    private static AILegacyPublicFileCleanup CreateCleanup(Mock<IFileSystem> fileSystem)
    {
        var mediaFileManager = new MediaFileManager(
            fileSystem.Object,
            Mock.Of<IMediaPathScheme>(),
            NullLogger<MediaFileManager>.Instance,
            Mock.Of<IShortStringHelper>(),
            Mock.Of<IServiceProvider>(),
            new Lazy<ICoreScopeProvider>(() => Mock.Of<ICoreScopeProvider>()));

        return new AILegacyPublicFileCleanup(mediaFileManager, NullLogger<AILegacyPublicFileCleanup>.Instance);
    }
}
