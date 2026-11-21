using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.AI.Core.Tools.Web;

/// <summary>
/// Fetches and extracts content from web pages with caching.
/// </summary>
public class WebContentFetcher : IWebContentFetcher
{
    private static readonly string[] AllowedContentTypes = {
        "text/html",
        "text/plain",
        "application/xhtml+xml"
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUrlValidator _urlValidator;
    private readonly IHtmlContentExtractor _htmlExtractor;
    private readonly IMemoryCache _cache;
    private readonly AIWebFetchOptions _options;
    private readonly ILogger<WebContentFetcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebContentFetcher"/> class.
    /// </summary>
    public WebContentFetcher(
        IHttpClientFactory httpClientFactory,
        IUrlValidator urlValidator,
        IHtmlContentExtractor htmlExtractor,
        IMemoryCache cache,
        IOptions<AIWebFetchOptions> options,
        ILogger<WebContentFetcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlValidator = urlValidator;
        _htmlExtractor = htmlExtractor;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FetchWebPageResult> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate URL
            var validationResult = await _urlValidator.ValidateAsync(url, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("URL validation failed for {Url}: {Error}", url, validationResult.ErrorMessage);
                return new FetchWebPageResult(false, null, $"Invalid URL: {validationResult.ErrorMessage}");
            }

            var normalizedUrl = validationResult.NormalizedUrl!;

            // Check cache
            if (_options.EnableCaching)
            {
                var cacheKey = $"WebFetch:{normalizedUrl}";
                if (_cache.TryGetValue<FetchWebPageResult>(cacheKey, out var cached) && cached is not null)
                {
                    _logger.LogInformation("Cache hit for URL: {Url}", normalizedUrl);
                    return cached;
                }

                // Fetch and cache
                var result = await FetchInternalAsync(normalizedUrl, cancellationToken);

                if (result.Success)
                {
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
                    _logger.LogInformation("Cached result for URL: {Url} (expires in {Minutes} minutes)",
                        normalizedUrl, _options.CacheDurationMinutes);
                }

                return result;
            }

            // No caching
            return await FetchInternalAsync(normalizedUrl, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Request timed out for URL: {Url}", url);
            return new FetchWebPageResult(false, null, "Request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching URL: {Url}", url);
            return new FetchWebPageResult(false, null, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Internal method to fetch content without caching logic.
    /// </summary>
    private async Task<FetchWebPageResult> FetchInternalAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("WebFetchTool");
            var currentUrl = url;

            for (var redirects = 0; ; redirects++)
            {
                // Fetch headers first to check content-type and size
                using var response = await httpClient.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // A handler that follows redirects reports the address it actually reached here, and that
                // address must pass the same validation as the URL the model supplied. Compare URIs, not
                // strings: RequestUri is the escaped canonical form, so a raw URL with spaces or non-ASCII
                // characters would otherwise look like a different address on every plain fetch. When
                // either side cannot be parsed the comparison fails and the reached address is validated.
                var reachedUri = response.RequestMessage?.RequestUri;
                var sameTarget = reachedUri is not null
                    && Uri.TryCreate(currentUrl, UriKind.Absolute, out var requestedUri)
                    && Uri.Compare(reachedUri, requestedUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;

                if (reachedUri is not null && !sameTarget)
                {
                    var reachedUrl = reachedUri.ToString();
                    var hopValidation = await _urlValidator.ValidateAsync(reachedUrl, cancellationToken);
                    if (!hopValidation.IsValid)
                    {
                        _logger.LogWarning("Redirect target validation failed for {Url}: {Error}", reachedUrl, hopValidation.ErrorMessage);
                        return new FetchWebPageResult(false, null, $"Invalid redirect target: {hopValidation.ErrorMessage}");
                    }

                    currentUrl = hopValidation.NormalizedUrl ?? reachedUrl;
                }

                // Follow redirects here rather than in the handler, so every hop is validated before it is fetched.
                if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found
                        or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect
                    && response.Headers.Location is not null)
                {
                    if (redirects >= _options.MaxRedirects)
                    {
                        _logger.LogWarning("Too many redirects fetching URL: {Url}", url);
                        return new FetchWebPageResult(false, null, $"Too many redirects (maximum {_options.MaxRedirects})");
                    }

                    var redirectUrl = new Uri(new Uri(currentUrl), response.Headers.Location).ToString();
                    var redirectValidation = await _urlValidator.ValidateAsync(redirectUrl, cancellationToken);
                    if (!redirectValidation.IsValid)
                    {
                        _logger.LogWarning("Redirect target validation failed for {Url}: {Error}", redirectUrl, redirectValidation.ErrorMessage);
                        return new FetchWebPageResult(false, null, $"Invalid redirect target: {redirectValidation.ErrorMessage}");
                    }

                    currentUrl = redirectValidation.NormalizedUrl ?? redirectUrl;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HTTP error {StatusCode} fetching URL: {Url}",
                        (int)response.StatusCode, url);
                    return new FetchWebPageResult(false, null,
                        $"HTTP error: {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                // Check content-type
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType == null || !AllowedContentTypes.Any(allowed =>
                    contentType.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("Unsupported content type {ContentType} for URL: {Url}", contentType, url);
                    return new FetchWebPageResult(false, null,
                        $"Unsupported content type: {contentType ?? "unknown"}. Only HTML and plain text are supported.");
                }

                // Check content length
                if (response.Content.Headers.ContentLength.HasValue &&
                    response.Content.Headers.ContentLength.Value > _options.MaxResponseSizeBytes)
                {
                    _logger.LogWarning("Response size {Size} exceeds limit {Limit} for URL: {Url}",
                        response.Content.Headers.ContentLength.Value, _options.MaxResponseSizeBytes, url);
                    return new FetchWebPageResult(false, null,
                        $"Response size exceeds maximum allowed size of {_options.MaxResponseSizeBytes} bytes");
                }

                // Read content with size limit
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var limitedStream = new LimitedStream(stream, _options.MaxResponseSizeBytes);
                using var reader = new StreamReader(limitedStream);

                var html = await reader.ReadToEndAsync(cancellationToken);

                // Extract content
                var extracted = await _htmlExtractor.ExtractAsync(html, url);

                // Calculate word count
                var wordCount = CountWords(extracted.TextContent);

                var content = new WebPageContent(
                    url,
                    extracted.Title,
                    extracted.TextContent,
                    extracted.Excerpt,
                    wordCount,
                    DateTime.UtcNow);

                _logger.LogInformation("Successfully fetched URL: {Url} (size: {Size} bytes, words: {Words})",
                    url, html.Length, wordCount);

                return new FetchWebPageResult(true, content, null);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Response size exceeds"))
        {
            _logger.LogWarning("Response size limit exceeded for URL: {Url}", url);
            return new FetchWebPageResult(false, null, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed for URL: {Url}", url);
            return new FetchWebPageResult(false, null, $"Failed to fetch URL: {ex.Message}");
        }
    }

    /// <summary>
    /// Counts words in text content.
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Regex.Matches(text, @"\b\w+\b").Count;
    }
}
