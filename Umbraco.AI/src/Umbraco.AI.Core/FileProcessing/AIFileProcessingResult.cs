namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// The result of processing a file for text extraction.
/// </summary>
/// <param name="Content">The extracted text content.</param>
/// <param name="WasTruncated">Whether the content was truncated due to size limits.</param>
public record AIFileProcessingResult(string Content, bool WasTruncated);
