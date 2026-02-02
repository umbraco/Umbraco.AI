using Microsoft.Extensions.Options;

namespace Umbraco.AI.Core.Tools.Web;

/// <summary>
/// Tool that safely fetches and extracts text content from web pages.
/// </summary>
[AITool("fetch_webpage", "Fetch Web Page", Category = "Web")]
public class FetchWebPageTool : AIToolBase<FetchWebPageArgs>
{
    private readonly IWebContentFetcher _fetcher;
    private readonly AIWebFetchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FetchWebPageTool"/> class.
    /// </summary>
    /// <param name="fetcher">The web content fetcher.</param>
    /// <param name="options">Web fetch options.</param>
    public FetchWebPageTool(
        IWebContentFetcher fetcher,
        IOptions<AIWebFetchOptions> options)
    {
        _fetcher = fetcher;
        _options = options.Value;
    }

    /// <inheritdoc />
    public override string Description =>
        "Fetches a web page from the provided URL and extracts the main text content. " +
        "Removes scripts, styles, navigation, ads, and other non-content elements. " +
        "Only supports public HTTP/HTTPS URLs - cannot access localhost or private networks. " +
        "Returns the page title, full text content, an excerpt, and word count.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(
        FetchWebPageArgs args,
        CancellationToken cancellationToken = default)
    {
        // Check if tool is enabled
        if (!_options.Enabled)
        {
            return new FetchWebPageResult(
                false,
                null,
                "Web fetch tool is disabled in configuration.");
        }

        // Fetch and extract
        return await _fetcher.FetchAsync(args.Url, cancellationToken);
    }
}
