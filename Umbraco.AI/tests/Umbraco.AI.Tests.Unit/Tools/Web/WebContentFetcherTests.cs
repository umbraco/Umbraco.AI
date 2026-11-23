using System.Net;
using System.Text;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Umbraco.AI.Core.Tools.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Web;

public class WebContentFetcherTests
{
    // Public IP literals so the real validator runs without DNS.
    private const string PublicUrl = "http://93.184.216.34/start";

    private readonly StubHandler _handler = new();
    private readonly AIWebFetchOptions _options = new() { EnableCaching = false, MaxRedirects = 2 };
    private readonly WebContentFetcher _fetcher;

    public WebContentFetcherTests()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("WebFetchTool")).Returns(() => new HttpClient(_handler, disposeHandler: false));

        var extractor = new Mock<IHtmlContentExtractor>();
        extractor.Setup(x => x.ExtractAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string html, string _) => new ExtractedContent("Title", html, html));

        var options = Options.Create(_options);
        _fetcher = new WebContentFetcher(
            factory.Object,
            new UrlValidator(options),
            extractor.Object,
            new MemoryCache(new MemoryCacheOptions()),
            options,
            NullLogger<WebContentFetcher>.Instance);
    }

    [Fact]
    public async Task FetchAsync_WithPublicAddress_ReturnsContent()
    {
        _handler.Respond(PublicUrl, Page("hello world"));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeTrue();
        result.Content!.TextContent.ShouldBe("hello world");
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://169.254.169.254/latest/")]
    [InlineData("http://[::ffff:10.0.0.1]/")]
    public async Task FetchAsync_WhenRedirectedToPrivateAddress_RefusesWithoutFetchingIt(string target)
    {
        _handler.Respond(PublicUrl, Redirect(target));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldStartWith("Invalid redirect target");
        _handler.Requested.ShouldBe([PublicUrl]);
    }

    [Fact]
    public async Task FetchAsync_WithRelativeRedirect_FollowsItOnTheSameHost()
    {
        _handler.Respond(PublicUrl, Redirect("/next?page=2"));
        _handler.Respond("http://93.184.216.34/next?page=2", Page("second page"));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeTrue();
        result.Content!.TextContent.ShouldBe("second page");
    }

    [Fact]
    public async Task FetchAsync_WithMoreRedirectsThanAllowed_Fails()
    {
        _handler.Respond(PublicUrl, Redirect("/a"));
        _handler.Respond("http://93.184.216.34/a", Redirect("/b"));
        _handler.Respond("http://93.184.216.34/b", Redirect("/c"));
        _handler.Respond("http://93.184.216.34/c", Page("too far"));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("Too many redirects");
        _handler.Requested.Count.ShouldBe(3);
    }

    [Fact]
    public async Task FetchAsync_WithRedirectsWithinLimit_Succeeds()
    {
        _handler.Respond(PublicUrl, Redirect("/a"));
        _handler.Respond("http://93.184.216.34/a", Redirect("http://93.184.216.35/b"));
        _handler.Respond("http://93.184.216.35/b", Page("done"));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeTrue();
        result.Content!.TextContent.ShouldBe("done");
    }

    [Fact]
    public async Task FetchAsync_WhenRedirectedFromHttpsToHttp_Refuses()
    {
        const string secureUrl = "https://93.184.216.34/start";
        _handler.Respond(secureUrl, Redirect("http://93.184.216.34/plain"));
        _handler.Respond("http://93.184.216.34/plain", Page("plain"));

        var result = await _fetcher.FetchAsync(secureUrl);

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("HTTPS to HTTP");
        _handler.Requested.ShouldBe([secureUrl]);
    }

    [Fact]
    public async Task FetchAsync_WhenHopsTogetherExceedTimeout_TimesOut()
    {
        // Each hop is under the timeout on its own; together they exceed it.
        _options.TimeoutSeconds = 1;
        _handler.Delay = TimeSpan.FromMilliseconds(600);
        _handler.Respond(PublicUrl, Redirect("/a"));
        _handler.Respond("http://93.184.216.34/a", Page("late"));

        var result = await _fetcher.FetchAsync(PublicUrl);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("Request timed out");
    }

    [Fact]
    public async Task FetchAsync_WhenCallerCancelsMidRedirect_ThrowsCancellation()
    {
        using var caller = new CancellationTokenSource();
        _handler.Respond(PublicUrl, Redirect("/a"));
        _handler.Respond("http://93.184.216.34/a", () =>
        {
            caller.Cancel();
            return null;
        });

        await Should.ThrowAsync<OperationCanceledException>(() => _fetcher.FetchAsync(PublicUrl, caller.Token));
        _handler.Requested.Count.ShouldBe(2);
    }

    private static Func<HttpResponseMessage> Page(string text) => () => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(text, Encoding.UTF8, "text/html"),
    };

    private static Func<HttpResponseMessage> Redirect(string location) => () =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.Found);
        response.Headers.Location = new Uri(location, UriKind.RelativeOrAbsolute);
        return response;
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Func<HttpResponseMessage?>> _responses = new();

        public TimeSpan Delay { get; set; }

        public List<string> Requested { get; } = [];

        public void Respond(string url, Func<HttpResponseMessage?> response) => _responses[url] = response;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requested.Add(url);
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            var response = _responses.TryGetValue(url, out var respond)
                ? respond()
                : new HttpResponseMessage(HttpStatusCode.NotFound);
            if (response is null)
            {
                // A null response means "hang until cancelled".
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return response!;
        }
    }
}
