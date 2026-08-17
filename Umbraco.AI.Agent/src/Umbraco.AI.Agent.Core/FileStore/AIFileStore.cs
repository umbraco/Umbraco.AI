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
/// </remarks>
internal sealed class AIFileStore : IAIFileStore
{
    private const string BasePath = "agui-files";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AIFileStore> _logger;
    private readonly IBackOfficeSecurityAccessor? _backOfficeSecurityAccessor;

    public AIFileStore(
        IFileSystem fileSystem,
        ILogger<AIFileStore> logger,
        IBackOfficeSecurityAccessor? backOfficeSecurityAccessor = null)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
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
    public Task<int> CleanupExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        if (!_fileSystem.DirectoryExists(BasePath))
        {
            return Task.FromResult(0);
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

            if (lastModified < cutoff)
            {
                _fileSystem.DeleteDirectory(threadDir, recursive: true);
                deleted++;
                _logger.LogDebug("Cleaned up expired thread directory {ThreadDir} (last modified: {LastModified})", threadDir, lastModified);
            }
        }

        return Task.FromResult(deleted);
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
