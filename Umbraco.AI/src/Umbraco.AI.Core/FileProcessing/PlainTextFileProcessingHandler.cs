using System.Text;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Extracts text content from plain-text files (CSV, Markdown, plain text).
/// </summary>
internal sealed class PlainTextFileProcessingHandler : IAIFileProcessingHandler
{
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "text/markdown",
    };

    /// <inheritdoc />
    public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
        => Task.FromResult(SupportedMimeTypes.Contains(mimeType));

    /// <inheritdoc />
    public Task<AIFileProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> data,
        string mimeType,
        string? filename,
        CancellationToken cancellationToken = default)
    {
        var content = Encoding.UTF8.GetString(data.Span);

        var wasTruncated = content.Length > AIFileProcessingConstants.MaxExtractedCharacters;
        if (wasTruncated)
        {
            content = content[..AIFileProcessingConstants.MaxExtractedCharacters] + "\n\n[Content truncated due to size limits]";
        }

        return Task.FromResult(new AIFileProcessingResult(content, wasTruncated));
    }
}
