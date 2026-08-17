using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.FileStore;

/// <summary>
/// Conversation uploads are private user content, so where they are stored is a security property, not
/// a detail. They were originally written to the media file system, which is rooted inside the web root
/// and served at <c>/media</c>, meaning every upload was downloadable anonymously no matter what the
/// management API enforced. Authenticating the endpoint did not change that, because the bytes were
/// reachable without going through the endpoint at all.
/// </summary>
/// <remarks>
/// These tests pin the location rather than the controller. A test that only exercised the endpoint
/// would have passed throughout the period the files were public.
/// </remarks>
public class AIFileStoreLocationTests
{
    [Fact]
    public void ConversationFilesDirectory_IsUnderTheContentRoot_NotTheWebRoot()
    {
        // Arrange & Act
        var path = AgentConstants.SystemDirectories.ConversationFiles;

        // Assert
        // "~/umbraco/..." is content-root relative. Anything under wwwroot is served statically.
        path.ShouldStartWith("~/umbraco/");
        path.ShouldNotContain("wwwroot");
        path.ShouldNotContain("media");
    }

    [Fact]
    public void StoreAsync_WritesBelowTheGivenFileSystemRoot_AndNeverAsksItForAPublicUrl()
    {
        // Arrange
        // If the store ever asked its file system for a URL, that would imply the content is
        // addressable. It must not, and the registration deliberately supplies an unusable root URL.
        var writtenPaths = new List<string>();
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem
            .Setup(x => x.AddFile(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
            .Callback<string, Stream, bool>((path, _, _) => writtenPaths.Add(path));

        var store = new AIFileStore(fileSystem.Object, NullLogger<AIFileStore>.Instance);

        // Act
        var fileId = store.StoreAsync("thread-1", [1, 2, 3], "image/png", "a.png").GetAwaiter().GetResult();

        // Assert
        writtenPaths.Count.ShouldBe(2);
        writtenPaths.ShouldAllBe(p => p.StartsWith("agui-files/thread-1/"));
        writtenPaths.ShouldContain($"agui-files/thread-1/{fileId}.bin");

        // Strict mock: any call to GetUrl (or anything else unconfigured) would have thrown by now.
        fileSystem.Verify(x => x.GetUrl(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PhysicalStoreRoot_ResolvesOutsideTheWebRoot()
    {
        // Arrange
        // Mirrors what the DI registration does, and asserts the property that actually matters: the
        // resolved directory is not somewhere the static file middleware will serve.
        var contentRoot = Path.Combine(Path.GetTempPath(), "uai-agent-location-test");
        var webRoot = Path.Combine(contentRoot, "wwwroot");

        var hostingEnvironment = new Mock<IHostingEnvironment>();
        hostingEnvironment
            .Setup(x => x.MapPathContentRoot(It.IsAny<string>()))
            .Returns<string>(p => Path.Combine(contentRoot, p.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar)));

        // Act
        var resolved = hostingEnvironment.Object
            .MapPathContentRoot(AgentConstants.SystemDirectories.ConversationFiles);

        // Assert
        resolved.ShouldStartWith(contentRoot);
        resolved.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
            "conversation uploads must not resolve inside the web root, which is served statically");
    }

    [Fact]
    public void LegacyPublicDirectory_IsStillKnown_SoTheUpgradeCanDeleteIt()
    {
        // Arrange & Act
        var legacy = AgentConstants.SystemDirectories.LegacyPublicConversationFiles;

        // Assert
        // Moving new uploads elsewhere leaves old ones sitting in the public media directory, and the
        // retention job only looks at the store's own file system. The constant has to stay so the
        // cleanup keeps working; dropping it would leave those files public indefinitely.
        legacy.ShouldBe("agui-files");
    }
}
