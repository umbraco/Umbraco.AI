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
        // One timeout for the whole fetch: validation, every redirect hop and the body read.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.TimeoutSeconds > 0)
        {
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        }

        var fetchToken = timeoutSource.Token;

        try
        {
            // Validate URL
            var validationResult = await _urlValidator.ValidateAsync(url, fetchToken);
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
                var result = await FetchInternalAsync(normalizedUrl, fetchToken);

                if (result.Success)
                {
                    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CacheDurationMinutes));
                    _logger.LogInformation("Cached result for URL: {Url} (expires in {Minutes} minutes)",
                        normalizedUrl, _options.CacheDurationMinutes);
                }

                return result;
            }

            // No caching
            return await FetchInternalAsync(normalizedUrl, fetchToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Cancelled by the timeout (or HttpClient.Timeout), not by the caller.
            _logger.LogWarning("Request timed out for URL: {Url}", url);
            return new FetchWebPageResult(false, null, "Request timed out");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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

            // Fetch headers first to check content-type and size. Redirects are followed here, not by the
            // handler, so every hop goes through the URL validator before it is fetched.
            var currentUrl = url;
            var response = await httpClient.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            for (var redirects = 0; IsRedirect(response); redirects++)
            {
                using (response)
                {
                    if (redirects >= _options.MaxRedirects)
                    {
                        _logger.LogWarning("Too many redirects fetching URL: {Url}", url);
                        return new FetchWebPageResult(false, null,
                            $"Too many redirects (maximum {_options.MaxRedirects})");
                    }

                    // Location may be relative; resolve it against the URL that returned it.
                    var currentUri = new Uri(currentUrl);
                    var redirectUri = new Uri(currentUri, response.Headers.Location!);
                    if (currentUri.Scheme == Uri.UriSchemeHttps && redirectUri.Scheme != Uri.UriSchemeHttps)
                    {
                        _logger.LogWarning("Refused redirect from HTTPS to {Scheme} fetching URL: {Url}",
                            redirectUri.Scheme, url);
                        return new FetchWebPageResult(false, null,
                            "Invalid redirect target: redirects from HTTPS to HTTP are not followed");
                    }

                    var redirectUrl = redirectUri.AbsoluteUri;
                    var redirectValidation = await _urlValidator.ValidateAsync(redirectUrl, cancellationToken);
                    if (!redirectValidation.IsValid)
                    {
                        _logger.LogWarning("Redirect target validation failed for {Url}: {Error}",
                            redirectUrl, redirectValidation.ErrorMessage);
                        return new FetchWebPageResult(false, null,
                            $"Invalid redirect target: {redirectValidation.ErrorMessage}");
                    }

                    currentUrl = redirectValidation.NormalizedUrl!;
                }

                response = await httpClient.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }

            using var _ = response;

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
            // Resolve relative links against the page actually fetched, after any redirects.
            var extracted = await _htmlExtractor.ExtractAsync(html, currentUrl);

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

    private static bool IsRedirect(HttpResponseMessage response)
        => response.Headers.Location is not null
           && response.StatusCode is HttpStatusCode.MovedPermanently
               or HttpStatusCode.Found
               or HttpStatusCode.SeeOther
               or HttpStatusCode.TemporaryRedirect
               or HttpStatusCode.PermanentRedirect;

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
