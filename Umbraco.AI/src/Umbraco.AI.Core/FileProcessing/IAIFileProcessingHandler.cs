namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Defines a handler that can extract text content from specific file types.
/// </summary>
/// <remarks>
/// Implementations handle specific MIME types (e.g., Office documents, CSV files) and convert
/// binary file data into text that can be understood by LLMs. Register handlers via
/// <c>builder.AIFileProcessingHandlers().Append&lt;MyHandler&gt;()</c> in a Composer.
/// </remarks>
public interface IAIFileProcessingHandler
{
    /// <summary>
    /// Determines whether this handler can process files of the specified MIME type.
    /// </summary>
    /// <remarks>
    /// This is asynchronous because a handler's eligibility can depend on runtime state
    /// (e.g., whether a default AI profile is configured), not just the MIME type alone.
    /// </remarks>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if this handler can process the MIME type; otherwise, <c>false</c>.</returns>
    Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text content from the given file data.
    /// </summary>
    /// <param name="data">The raw file bytes.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="filename">The optional filename for context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The extracted text content and whether it was truncated.</returns>
    Task<AIFileProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> data,
        string mimeType,
        string? filename,
        CancellationToken cancellationToken = default);
}
