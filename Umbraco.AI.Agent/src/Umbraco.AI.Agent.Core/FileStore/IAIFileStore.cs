namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Thread-scoped temporary storage for binary file content.
/// Stores files uploaded during a conversation and resolves them by ID on subsequent turns.
/// </summary>
public interface IAIFileStore
{
    /// <summary>
    /// Stores binary data and returns a unique file ID.
    /// </summary>
    /// <param name="threadId">The conversation thread ID for scoping.</param>
    /// <param name="data">The binary file data.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="filename">The original filename, if available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique file ID for referencing this file in subsequent turns.</returns>
    Task<string> StoreAsync(string threadId, byte[] data, string mimeType, string? filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a file ID to its stored data.
    /// </summary>
    /// <param name="threadId">The conversation thread ID for scoping.</param>
    /// <param name="fileId">The file ID returned by <see cref="StoreAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored file data, or <c>null</c> if not found.</returns>
    Task<AIStoredFile?> ResolveAsync(string threadId, string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up all files stored for a thread.
    /// </summary>
    /// <param name="threadId">The conversation thread ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupThreadAsync(string threadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up thread directories whose files have not been modified within the given retention period.
    /// </summary>
    /// <param name="maxAge">The maximum age of files before they are cleaned up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of thread directories deleted.</returns>
    Task<int> CleanupExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a file stored in the temporary file store.
/// </summary>
public sealed class AIStoredFile
{
    /// <summary>
    /// Gets the binary file data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Gets the original filename, if available.
    /// </summary>
    public string? Filename { get; init; }
}
