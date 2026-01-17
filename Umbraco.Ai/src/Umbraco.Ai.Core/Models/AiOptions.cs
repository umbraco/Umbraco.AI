namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Configuration options for AI services.
/// </summary>
public class AiOptions
{
    /// <summary>
    /// The default chat profile alias to use when none is specified.
    /// </summary>
    public string? DefaultChatProfileAlias { get; set; }

    /// <summary>
    /// The default embedding profile alias to use when none is specified.
    /// </summary>
    public string? DefaultEmbeddingProfileAlias { get; set; }

    // TODO: public string? DefaultImageProviderAlias { get; set; }
    // TODO: public string? DefaultModerationProviderAlias { get; set; }
    // TODO: public string? DefaultToolProviderAlias { get; set; }
}