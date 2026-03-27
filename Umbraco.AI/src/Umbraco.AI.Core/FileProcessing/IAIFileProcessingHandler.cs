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
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns><c>true</c> if this handler can process the MIME type; otherwise, <c>false</c>.</returns>
    bool CanHandle(string mimeType);

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
