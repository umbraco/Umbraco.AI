using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Implementation of <see cref="IAIFileStore"/> storing files in a thread-scoped directory under
/// <c>agui-files/</c> on the file system it is given.
/// </summary>
/// <remarks>
/// <para>
/// The file system MUST NOT be one that is served publicly. Conversation uploads are private user
/// content, and this store previously used <c>MediaFileManager.FileSystem</c>, which is rooted inside
/// the web root and served at <c>/media</c> — so every uploaded file was downloadable anonymously
/// regardless of what the management API allowed. It is now given a file system rooted outside the
/// web root by <c>AddUmbracoAIAgentCore</c>. Do not reintroduce a publicly served file system here.
/// </para>
/// <para>
/// Files are owned by the backoffice user who uploaded them. The owner is recorded on
/// <see cref="StoreAsync"/> and checked on <see cref="ResolveAsync"/>, so a file only ever resolves
/// for the user it belongs to. The check lives here rather than in the callers because there is more
/// than one resolve path — the file endpoint and follow-up turns that reference a file by id — and
/// both take a thread and file id supplied by the client.
/// </para>
/// <para>
/// <see cref="CleanupExpiredAsync"/> ages out a thread once it has been quiet past the retention
/// window, unless a registered <see cref="IAIFileThreadLifecycleProvider"/> reports the thread's backing
/// record is still alive (a persisted conversation, say) — that keeps a long-lived conversation's
/// attachments readable for as long as the conversation exists, instead of on a fixed clock that fits
/// only short-lived, unsaved chats. <see cref="CleanupThreadAsync"/> remains the explicit purge called
/// when a record is actually deleted.
/// </para>
/// <para>
/// A confirmed-alive thread gets a small lifecycle marker file, refreshed on each confirmation, so it
/// counts as the thread's most recently modified file. That keeps the thread under the sweep's own
/// cutoff — and so out of the provider check entirely — until the marker itself goes stale (roughly once
/// per retention window). Without it, a long-lived conversation would re-ask a provider on every single
/// hourly sweep for as long as it exists, for an answer that's essentially always the same.
/// </para>
/// </remarks>
internal sealed class AIFileStore : IAIFileStore
{
    private const string BasePath = "agui-files";

    /// <summary>
    /// A zero-content marker written into a thread directory once a lifecycle provider confirms it's
    /// still alive. Never resolvable as an attachment — real files are always named
    /// <c>file-&lt;guid&gt;.bin</c>/<c>.json</c>, so this name can never collide with one.
    /// </summary>
    internal const string LifecycleMarkerFileName = "lifecycle-marker.json";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AIFileStore> _logger;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;
    private readonly AIFileThreadLifecycleProviderCollection _lifecycleProviders;

    public AIFileStore(
        IFileSystem fileSystem,
        ILogger<AIFileStore> logger,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null,
        AIFileThreadLifecycleProviderCollection? lifecycleProviders = null)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _lifecycleProviders = lifecycleProviders ?? new AIFileThreadLifecycleProviderCollection(() => []);
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(string threadId, byte[] data, string mimeType, string? filename, CancellationToken cancellationToken = default)
    {
        var fileId = $"file-{Guid.NewGuid():N}";
        var threadDir = GetThreadPath(threadId);

        // Store data file
        var dataPath = $"{threadDir}/{fileId}.bin";
        using (var dataStream = new MemoryStream(data))
        {
            _fileSystem.AddFile(dataPath, dataStream, overrideIfExists: true);
        }

        // Store metadata
        var metaPath = $"{threadDir}/{fileId}.json";
        var metadata = new FileMetadata
        {
            MimeType = mimeType,
            Filename = filename,
            OwnerKey = GetCurrentUserKey()?.ToString(),
        };
        var metaJson = JsonSerializer.Serialize(metadata);
        using (var metaStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(metaJson)))
        {
            _fileSystem.AddFile(metaPath, metaStream, overrideIfExists: true);
        }

        _logger.LogDebug("Stored file {FileId} for thread {ThreadId} ({Size} bytes, {MimeType})",
            fileId, threadId, data.Length, mimeType);

        return fileId;
    }

    /// <inheritdoc />
    public async Task<AIStoredFile?> ResolveAsync(string threadId, string fileId, CancellationToken cancellationToken = default)
    {
        var threadDir = GetThreadPath(threadId);
        var dataPath = $"{threadDir}/{fileId}.bin";
        var metaPath = $"{threadDir}/{fileId}.json";

        // threadId and fileId reach us straight from a client-supplied route. The underlying file
        // system rejects any path that escapes its root, but it does so by throwing — translate that
        // into "no such file" rather than letting it surface as a server error.
        try
        {
            if (!_fileSystem.FileExists(dataPath) || !_fileSystem.FileExists(metaPath))
            {
                _logger.LogWarning("File {FileId} not found for thread {ThreadId}", fileId, threadId);
                return null;
            }

            FileMetadata? metadata;
            using (var metaStream = _fileSystem.OpenFile(metaPath))
            {
                metadata = await JsonSerializer.DeserializeAsync<FileMetadata>(metaStream, cancellationToken: cancellationToken);
            }

            if (!IsOwnedByCurrentUser(metadata, threadId, fileId))
            {
                return null;
            }

            byte[] data;
            using (var dataStream = _fileSystem.OpenFile(dataPath))
            using (var ms = new MemoryStream())
            {
                await dataStream.CopyToAsync(ms, cancellationToken);
                data = ms.ToArray();
            }

            return new AIStoredFile
            {
                Data = data,
                MimeType = metadata?.MimeType ?? "application/octet-stream",
                Filename = metadata?.Filename
            };
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning(
                "Rejected file request for thread {ThreadId} and file {FileId}: the resolved path is outside the file store root",
                threadId,
                fileId);
            return null;
        }
    }

    /// <summary>
    /// Checks the stored owner against the acting backoffice user. Fails closed: a file with no
    /// recorded owner does not resolve, so files written before ownership was recorded are treated as
    /// unreadable rather than readable by anyone. They age out via the retention job.
    /// </summary>
    private bool IsOwnedByCurrentUser(FileMetadata? metadata, string threadId, string fileId)
    {
        var currentUserKey = GetCurrentUserKey();
        if (currentUserKey is null)
        {
            _logger.LogWarning(
                "Refusing to resolve file {FileId} for thread {ThreadId}: no backoffice user on the request",
                fileId,
                threadId);
            return false;
        }

        if (string.IsNullOrEmpty(metadata?.OwnerKey))
        {
            _logger.LogWarning(
                "Refusing to resolve file {FileId} for thread {ThreadId}: no owner recorded against the file",
                fileId,
                threadId);
            return false;
        }

        if (!string.Equals(metadata.OwnerKey, currentUserKey.Value.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Refusing to resolve file {FileId} for thread {ThreadId}: it belongs to a different user",
                fileId,
                threadId);
            return false;
        }

        return true;
    }

    private Guid? GetCurrentUserKey()
        => _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser?.Key;

    /// <inheritdoc />
    public Task CleanupThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var threadDir = GetThreadPath(threadId);
        if (_fileSystem.DirectoryExists(threadDir))
        {
            _fileSystem.DeleteDirectory(threadDir, recursive: true);
            _logger.LogDebug("Cleaned up files for thread {ThreadId}", threadId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.DirectoryExists(BasePath))
        {
            return 0;
        }

        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var deleted = 0;

        foreach (var threadDir in _fileSystem.GetDirectories(BasePath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check the most recent file modification in the thread directory
            var files = _fileSystem.GetFiles(threadDir).ToList();
            if (files.Count == 0)
            {
                // Empty directory — clean up
                _fileSystem.DeleteDirectory(threadDir, recursive: true);
                deleted++;
                continue;
            }

            var lastModified = files
                .Select(f => _fileSystem.GetLastModified(f))
                .Max();

            if (lastModified >= cutoff)
            {
                continue;
            }

            var threadId = threadDir[(BasePath.Length + 1)..];
            if (await IsThreadStillAliveAsync(threadId, cancellationToken))
            {
                // A registered lifecycle provider says this thread's backing record still exists (a
                // persisted conversation, say) — keep it no matter how old. It is purged via
                // CleanupThreadAsync when that record is actually deleted, not on a fixed clock.
                //
                // Refresh the lifecycle marker so it counts as the newest file here next sweep. That
                // keeps this thread under the cutoff above without reaching this point again until the
                // marker itself goes stale (roughly once per retention window) — a long-lived
                // conversation would otherwise re-ask the provider every single sweep for its entire
                // life, for an answer that is essentially always the same.
                TouchLifecycleMarker(threadDir);
                continue;
            }

            _fileSystem.DeleteDirectory(threadDir, recursive: true);
            deleted++;
            _logger.LogDebug("Cleaned up expired thread directory {ThreadDir} (last modified: {LastModified})", threadDir, lastModified);
        }

        return deleted;
    }

    /// <summary>
    /// Asks every registered <see cref="IAIFileThreadLifecycleProvider"/> whether it still owns a live
    /// record for this thread. Fails closed: a provider that throws is treated as "keep it this pass"
    /// rather than "delete it", so a transient fault (the database being briefly unreachable during the
    /// hourly sweep, say) cannot delete a live persisted conversation's attachments.
    /// </summary>
    private async Task<bool> IsThreadStillAliveAsync(string threadId, CancellationToken cancellationToken)
    {
        foreach (var provider in _lifecycleProviders)
        {
            AIFileThreadLifecycleStatus status;
            try
            {
                status = await provider.GetStatusAsync(threadId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "File thread lifecycle provider {Provider} failed checking thread {ThreadId}; keeping it this pass",
                    provider.GetType().Name,
                    threadId);
                return true;
            }

            if (status == AIFileThreadLifecycleStatus.Alive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rewrites the lifecycle marker so its timestamp becomes "now". A write failure is logged and
    /// swallowed rather than thrown — this runs inside the sweep's loop over every thread directory, and
    /// one directory's write hiccup (a permissions issue, a full disk) must not abort the pass for every
    /// other directory still waiting to be checked. Worst case, the next sweep just re-asks the provider
    /// again for this thread, the same fail-safe fallback as a provider itself failing.
    /// </summary>
    private void TouchLifecycleMarker(string threadDir)
    {
        try
        {
            var markerPath = $"{threadDir}/{LifecycleMarkerFileName}";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
            _fileSystem.AddFile(markerPath, stream, overrideIfExists: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to refresh the lifecycle marker for thread directory {ThreadDir}", threadDir);
        }
    }

    private static string GetThreadPath(string threadId)
        => $"{BasePath}/{threadId}";

    private sealed class FileMetadata
    {
        public string MimeType { get; set; } = "application/octet-stream";
        public string? Filename { get; set; }

        /// <summary>
        /// Key of the backoffice user who uploaded the file. Absent on files written before ownership
        /// was recorded; those no longer resolve (see <see cref="IsOwnedByCurrentUser"/>).
        /// </summary>
        public string? OwnerKey { get; set; }
    }
}
