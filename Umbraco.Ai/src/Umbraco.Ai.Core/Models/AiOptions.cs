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

    /// <summary>
    /// AI testing configuration options.
    /// </summary>
    public AiTestOptions Test { get; set; } = new();
}

/// <summary>
/// Configuration options for AI testing.
/// </summary>
public class AiTestOptions
{
    /// <summary>
    /// Number of test runs to keep per test (default: 100).
    /// Older runs are automatically deleted to prevent unbounded growth.
    /// Set to 0 to disable automatic cleanup.
    /// </summary>
    public int RunRetentionCount { get; set; } = 100;
}