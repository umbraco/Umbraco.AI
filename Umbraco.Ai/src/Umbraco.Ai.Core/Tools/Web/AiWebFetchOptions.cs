namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Configuration options for web fetching.
/// </summary>
public class AiWebFetchOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the web fetch tool is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum response size in bytes (default: 5 MB).
    /// </summary>
    public long MaxResponseSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the request timeout in seconds (default: 30).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of redirects to follow (default: 5).
    /// </summary>
    public int MaxRedirects { get; set; } = 5;

    /// <summary>
    /// Gets or sets the domain whitelist (if specified, only these domains are allowed).
    /// Empty list means no whitelist filtering.
    /// </summary>
    public List<string> AllowedDomains { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain blacklist (blocks specific domains even if not in private ranges).
    /// </summary>
    public List<string> BlockedDomains { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether caching of fetched content is enabled (default: true).
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes (default: 60).
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;
}
