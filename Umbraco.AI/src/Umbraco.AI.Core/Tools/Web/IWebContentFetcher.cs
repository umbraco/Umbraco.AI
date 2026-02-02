namespace Umbraco.AI.Core.Tools.Web;

/// <summary>
/// Fetches and extracts content from web pages.
/// </summary>
public interface IWebContentFetcher
{
    /// <summary>
    /// Fetches a web page and extracts its content.
    /// </summary>
    /// <param name="url">The URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fetch result.</returns>
    Task<FetchWebPageResult> FetchAsync(string url, CancellationToken cancellationToken = default);
}
