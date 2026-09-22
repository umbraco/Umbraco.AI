using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Media;

/// <summary>
/// Maps file extensions to the MIME types <see cref="IAIUmbracoMediaResolver"/> understands.
/// Extracted from <see cref="AIUmbracoMediaResolver"/> so the lookup can be unit tested without
/// the Umbraco CMS media/file-system infrastructure that resolver depends on.
/// </summary>
internal static class AIMediaExtensionResolver
{
    private static readonly Dictionary<string, string> ExtensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".bmp"] = "image/bmp",

        // Audio
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".webm"] = "audio/webm",
        [".flac"] = "audio/flac",

        // Plain text
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",

        // Office documents
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
    };

    /// <summary>
    /// Attempts to resolve the MIME type for a file extension (e.g. <c>.png</c>).
    /// </summary>
    /// <param name="extension">The file extension, including the leading dot.</param>
    /// <param name="mediaType">The resolved MIME type, when found.</param>
    /// <returns><c>true</c> if the extension is recognized; otherwise <c>false</c>.</returns>
    public static bool TryGetMediaType(string extension, [NotNullWhen(true)] out string? mediaType)
        => ExtensionToMediaType.TryGetValue(extension, out mediaType);
}
