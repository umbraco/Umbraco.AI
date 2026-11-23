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
        private readonly Dictionary<string, Func<HttpResponseMessage>> _responses = new();

        public List<string> Requested { get; } = [];

        public void Respond(string url, Func<HttpResponseMessage> response) => _responses[url] = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requested.Add(url);
            return Task.FromResult(_responses.TryGetValue(url, out var response)
                ? response()
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
