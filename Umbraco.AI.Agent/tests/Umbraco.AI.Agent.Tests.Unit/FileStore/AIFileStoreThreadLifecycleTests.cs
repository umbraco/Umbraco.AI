using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.IO;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.FileStore;

/// <summary>
/// A fixed-age retention sweep is right for a plain, unsaved chat thread but wrong for one backed by a
/// persisted conversation, which can legitimately sit untouched for a long time and still needs its
/// attachments to resolve. These tests pin the sweep's behaviour when a
/// <see cref="IAIFileThreadLifecycleProvider"/> is in play, rather than the provider's own logic (covered
/// separately in the Conversations project).
/// </summary>
public class AIFileStoreThreadLifecycleTests
{
    private const string ThreadDir = "agui-files/thread-1";
    private const string ThreadId = "thread-1";

    private static Mock<IFileSystem> CreateFileSystemWithOneExpiredThread()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);
        fileSystem.Setup(x => x.GetDirectories("agui-files")).Returns([ThreadDir]);
        fileSystem.Setup(x => x.GetFiles(ThreadDir)).Returns([$"{ThreadDir}/file-1.bin"]);
        fileSystem.Setup(x => x.GetLastModified(It.IsAny<string>())).Returns(DateTimeOffset.UtcNow - TimeSpan.FromDays(2));
        return fileSystem;
    }

    private static AIFileThreadLifecycleProviderCollection CollectionOf(params IAIFileThreadLifecycleProvider[] providers)
        => new(() => providers);

    private static Mock<IAIFileThreadLifecycleProvider> CreateProvider(AIFileThreadLifecycleStatus status)
    {
        var provider = new Mock<IAIFileThreadLifecycleProvider>();
        provider.Setup(x => x.GetStatusAsync(ThreadId, It.IsAny<CancellationToken>())).ReturnsAsync(status);
        return provider;
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithNoProviders_DeletesTheExpiredThread()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var store = new AIFileStore(fileSystem.Object, NullLogger<AIFileStore>.Instance);

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(1);
        fileSystem.Verify(x => x.DeleteDirectory(ThreadDir, true), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WhenAProviderReportsAlive_KeepsTheThreadRegardlessOfAge()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var provider = CreateProvider(AIFileThreadLifecycleStatus.Alive);
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(provider.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(0);
        fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WhenAProviderReportsGone_DeletesTheThread()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var provider = CreateProvider(AIFileThreadLifecycleStatus.Gone);
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(provider.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(1);
        fileSystem.Verify(x => x.DeleteDirectory(ThreadDir, true), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WhenUnclaimedByEveryProvider_DeletesTheThread()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var provider = CreateProvider(AIFileThreadLifecycleStatus.Unclaimed);
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(provider.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(1);
        fileSystem.Verify(x => x.DeleteDirectory(ThreadDir, true), Times.Once);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WhenAProviderThrows_KeepsTheThreadAndDoesNotThrow()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var provider = new Mock<IAIFileThreadLifecycleProvider>();
        provider
            .Setup(x => x.GetStatusAsync(ThreadId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unreachable"));
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(provider.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(0);
        fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CleanupExpiredAsync_WithMultipleProviders_AnyAliveVoteWins()
    {
        var fileSystem = CreateFileSystemWithOneExpiredThread();
        var unclaimed = CreateProvider(AIFileThreadLifecycleStatus.Unclaimed);
        var alive = CreateProvider(AIFileThreadLifecycleStatus.Alive);
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(unclaimed.Object, alive.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(0);
        fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ThreadNotYetPastRetentionWindow_IsNeverAskedOfProviders()
    {
        // A provider check is only meaningful (and only worth the cost) once age-based eligibility is
        // already established — a fresh thread is never a candidate regardless of what a provider says.
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);
        fileSystem.Setup(x => x.GetDirectories("agui-files")).Returns([ThreadDir]);
        fileSystem.Setup(x => x.GetFiles(ThreadDir)).Returns([$"{ThreadDir}/file-1.bin"]);
        fileSystem.Setup(x => x.GetLastModified(It.IsAny<string>())).Returns(DateTimeOffset.UtcNow);

        var provider = new Mock<IAIFileThreadLifecycleProvider>(MockBehavior.Strict);
        var store = new AIFileStore(
            fileSystem.Object,
            NullLogger<AIFileStore>.Instance,
            backOfficeSecurityAccessor: null,
            lifecycleProviders: CollectionOf(provider.Object));

        var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

        deleted.ShouldBe(0);
        fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Without this, a long-lived persisted conversation would re-ask a provider on every single hourly
    /// sweep for as long as it exists. The marker turns that into "roughly once per retention window" by
    /// making the thread look recently modified again as soon as a provider confirms it's alive.
    /// </summary>
    public class LifecycleMarker
    {
        private static readonly string MarkerPath = $"{ThreadDir}/{AIFileStore.LifecycleMarkerFileName}";

        [Fact]
        public async Task CleanupExpiredAsync_WhenAProviderReportsAlive_WritesTheLifecycleMarker()
        {
            var fileSystem = CreateFileSystemWithOneExpiredThread();
            var provider = CreateProvider(AIFileThreadLifecycleStatus.Alive);
            var store = new AIFileStore(
                fileSystem.Object,
                NullLogger<AIFileStore>.Instance,
                backOfficeSecurityAccessor: null,
                lifecycleProviders: CollectionOf(provider.Object));

            await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

            fileSystem.Verify(x => x.AddFile(MarkerPath, It.IsAny<Stream>(), true), Times.Once);
        }

        [Fact]
        public async Task CleanupExpiredAsync_WithAFreshMarker_NeverAsksTheProviderAgain()
        {
            // The real attachment is old, but the marker (written on a prior sweep) is not — the
            // directory as a whole still looks recently touched, so it never even reaches the provider.
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);
            fileSystem.Setup(x => x.GetDirectories("agui-files")).Returns([ThreadDir]);
            fileSystem.Setup(x => x.GetFiles(ThreadDir)).Returns([$"{ThreadDir}/file-1.bin", MarkerPath]);
            fileSystem.Setup(x => x.GetLastModified($"{ThreadDir}/file-1.bin")).Returns(DateTimeOffset.UtcNow - TimeSpan.FromDays(10));
            fileSystem.Setup(x => x.GetLastModified(MarkerPath)).Returns(DateTimeOffset.UtcNow - TimeSpan.FromHours(1));

            var provider = new Mock<IAIFileThreadLifecycleProvider>(MockBehavior.Strict);
            var store = new AIFileStore(
                fileSystem.Object,
                NullLogger<AIFileStore>.Instance,
                backOfficeSecurityAccessor: null,
                lifecycleProviders: CollectionOf(provider.Object));

            var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

            deleted.ShouldBe(0);
            fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task CleanupExpiredAsync_WithAStaleMarker_AsksTheProviderAgain()
        {
            // Both the attachment and the marker have gone past the retention window — time to re-verify
            // rather than trust a confirmation from a whole retention window ago.
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.DirectoryExists("agui-files")).Returns(true);
            fileSystem.Setup(x => x.GetDirectories("agui-files")).Returns([ThreadDir]);
            fileSystem.Setup(x => x.GetFiles(ThreadDir)).Returns([$"{ThreadDir}/file-1.bin", MarkerPath]);
            fileSystem.Setup(x => x.GetLastModified(It.IsAny<string>())).Returns(DateTimeOffset.UtcNow - TimeSpan.FromDays(2));

            var provider = CreateProvider(AIFileThreadLifecycleStatus.Alive);
            var store = new AIFileStore(
                fileSystem.Object,
                NullLogger<AIFileStore>.Instance,
                backOfficeSecurityAccessor: null,
                lifecycleProviders: CollectionOf(provider.Object));

            await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

            provider.Verify(x => x.GetStatusAsync(ThreadId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CleanupExpiredAsync_WhenWritingTheMarkerFails_KeepsTheThreadAndDoesNotThrow()
        {
            var fileSystem = CreateFileSystemWithOneExpiredThread();
            fileSystem
                .Setup(x => x.AddFile(MarkerPath, It.IsAny<Stream>(), true))
                .Throws(new IOException("disk full"));
            var provider = CreateProvider(AIFileThreadLifecycleStatus.Alive);
            var store = new AIFileStore(
                fileSystem.Object,
                NullLogger<AIFileStore>.Instance,
                backOfficeSecurityAccessor: null,
                lifecycleProviders: CollectionOf(provider.Object));

            var deleted = await store.CleanupExpiredAsync(TimeSpan.FromHours(24));

            deleted.ShouldBe(0);
            fileSystem.Verify(x => x.DeleteDirectory(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
