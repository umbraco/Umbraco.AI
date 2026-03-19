using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Agent.Core.AGUI;

/// <summary>
/// File-on-disk implementation of <see cref="IAGUIFileStore"/>.
/// Stores files in a temporary directory scoped by thread ID.
/// </summary>
internal sealed class AGUIFileStore : IAGUIFileStore
{
    private readonly string _basePath;
    private readonly ILogger<AGUIFileStore> _logger;

    public AGUIFileStore(IHostEnvironment hostEnvironment, ILogger<AGUIFileStore> logger)
    {
        _basePath = Path.Combine(hostEnvironment.ContentRootPath, "umbraco", "Data", "TEMP", "agui-files");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(string threadId, byte[] data, string mimeType, string? filename, CancellationToken cancellationToken = default)
    {
        var fileId = $"file-{Guid.NewGuid():N}";
        var threadDir = GetThreadDirectory(threadId);
        Directory.CreateDirectory(threadDir);

        // Store data file
        var dataPath = Path.Combine(threadDir, fileId + ".bin");
        await File.WriteAllBytesAsync(dataPath, data, cancellationToken);

        // Store metadata
        var metaPath = Path.Combine(threadDir, fileId + ".json");
        var metadata = new FileMetadata { MimeType = mimeType, Filename = filename };
        var metaJson = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metaPath, metaJson, cancellationToken);

        _logger.LogDebug("Stored file {FileId} for thread {ThreadId} ({Size} bytes, {MimeType})",
            fileId, threadId, data.Length, mimeType);

        return fileId;
    }

    /// <inheritdoc />
    public async Task<AGUIStoredFile?> ResolveAsync(string threadId, string fileId, CancellationToken cancellationToken = default)
    {
        var threadDir = GetThreadDirectory(threadId);
        var dataPath = Path.Combine(threadDir, fileId + ".bin");
        var metaPath = Path.Combine(threadDir, fileId + ".json");

        if (!File.Exists(dataPath) || !File.Exists(metaPath))
        {
            _logger.LogWarning("File {FileId} not found for thread {ThreadId}", fileId, threadId);
            return null;
        }

        var data = await File.ReadAllBytesAsync(dataPath, cancellationToken);
        var metaJson = await File.ReadAllTextAsync(metaPath, cancellationToken);
        var metadata = JsonSerializer.Deserialize<FileMetadata>(metaJson);

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
        var threadDir = GetThreadDirectory(threadId);
        if (Directory.Exists(threadDir))
        {
            Directory.Delete(threadDir, recursive: true);
            _logger.LogDebug("Cleaned up files for thread {ThreadId}", threadId);
        }

        return Task.CompletedTask;
    }

    private string GetThreadDirectory(string threadId)
        => Path.Combine(_basePath, threadId);

    private sealed class FileMetadata
    {
        public string MimeType { get; set; } = "application/octet-stream";
        public string? Filename { get; set; }
    }
}
