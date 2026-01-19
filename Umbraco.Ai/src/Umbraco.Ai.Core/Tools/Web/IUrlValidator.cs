namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Validates URLs for security concerns.
/// </summary>
public interface IUrlValidator
{
    /// <summary>
    /// Validates that a URL is safe to fetch.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with error message if invalid.</returns>
    Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of URL validation.
/// </summary>
/// <param name="IsValid">Whether the URL is valid and safe.</param>
/// <param name="ErrorMessage">Error message if invalid.</param>
/// <param name="NormalizedUrl">The normalized URL if valid.</param>
public record UrlValidationResult(
    bool IsValid,
    string? ErrorMessage,
    string? NormalizedUrl);
