namespace Umbraco.AI.AGUI.Models;

/// <summary>
/// Factory + classifier helpers for constructing the AG-UI typed content variants
/// from a MIME type. Mirrors the SDK's official mapping
/// (<c>image/*</c>, <c>audio/*</c>, <c>video/*</c>, else <c>document</c>).
/// </summary>
public static class AGUIInputContentFactory
{
    /// <summary>
    /// Classifies a MIME type into one of the AG-UI typed content kinds.
    /// </summary>
    public static AGUIInputContentKind Classify(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            return AGUIInputContentKind.Document;
        }

        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return AGUIInputContentKind.Image;
        }

        if (mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return AGUIInputContentKind.Audio;
        }

        if (mimeType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return AGUIInputContentKind.Video;
        }

        return AGUIInputContentKind.Document;
    }

    /// <summary>
    /// Wraps an <see cref="AGUIInputContentSource"/> in the typed content variant
    /// dictated by <paramref name="mimeType"/>, attaching optional metadata.
    /// </summary>
    public static AGUIInputContent FromSource(
        AGUIInputContentSource source,
        string? mimeType,
        IReadOnlyDictionary<string, object?>? metadata = null)
        => Classify(mimeType) switch
        {
            AGUIInputContentKind.Image => new AGUIImageInputContent { Source = source, Metadata = metadata },
            AGUIInputContentKind.Audio => new AGUIAudioInputContent { Source = source, Metadata = metadata },
            AGUIInputContentKind.Video => new AGUIVideoInputContent { Source = source, Metadata = metadata },
            _ => new AGUIDocumentInputContent { Source = source, Metadata = metadata },
        };
}

/// <summary>
/// AG-UI typed content kinds (the discriminator value on <c>AGUIInputContent.type</c>).
/// </summary>
public enum AGUIInputContentKind
{
    /// <summary>Image content (<c>image/*</c>).</summary>
    Image,

    /// <summary>Audio content (<c>audio/*</c>).</summary>
    Audio,

    /// <summary>Video content (<c>video/*</c>).</summary>
    Video,

    /// <summary>Document content — catch-all for non-media MIME types.</summary>
    Document
}
