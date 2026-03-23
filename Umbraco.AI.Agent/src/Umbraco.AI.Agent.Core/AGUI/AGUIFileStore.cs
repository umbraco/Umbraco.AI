using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.IO;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// Implementation of <see cref="IAGUIFileStore"/> backed by the Umbraco media file system.
/// Stores files in a thread-scoped directory under <c>agui-files/</c>, using <see cref="MediaFileManager.FileSystem"/>
/// so that storage is portable across providers (local disk, Azure Blob, S3, etc.).
/// </summary>
internal sealed class AGUIFileStore : IAGUIFileStore
{
    private const string BasePath = "agui-files";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<AGUIFileStore> _logger;

    public AGUIFileStore(MediaFileManager mediaFileManager, ILogger<AGUIFileStore> logger)
    {
        _fileSystem = mediaFileManager.FileSystem;
        _logger = logger;
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
        var metadata = new FileMetadata { MimeType = mimeType, Filename = filename };
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
    public async Task<AGUIStoredFile?> ResolveAsync(string threadId, string fileId, CancellationToken cancellationToken = default)
    {
        var threadDir = GetThreadPath(threadId);
        var dataPath = $"{threadDir}/{fileId}.bin";
        var metaPath = $"{threadDir}/{fileId}.json";

        if (!_fileSystem.FileExists(dataPath) || !_fileSystem.FileExists(metaPath))
        {
            _logger.LogWarning("File {FileId} not found for thread {ThreadId}", fileId, threadId);
            return null;
        }

        byte[] data;
        using (var dataStream = _fileSystem.OpenFile(dataPath))
        using (var ms = new MemoryStream())
        {
            await dataStream.CopyToAsync(ms, cancellationToken);
            data = ms.ToArray();
        }

        FileMetadata? metadata;
        using (var metaStream = _fileSystem.OpenFile(metaPath))
        {
            metadata = await JsonSerializer.DeserializeAsync<FileMetadata>(metaStream, cancellationToken: cancellationToken);
        }

        return new AGUIStoredFile
        {
            Data = data,
            MimeType = metadata?.MimeType ?? "application/octet-stream",
            Filename = metadata?.Filename
        };
    }

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

    private static string GetThreadPath(string threadId)
        => $"{BasePath}/{threadId}";

    private sealed class FileMetadata
    {
        public string MimeType { get; set; } = "application/octet-stream";
        public string? Filename { get; set; }
    }
}
