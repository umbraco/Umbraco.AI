using System.Net;

using Umbraco.AI.Core.Tools.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Web;

public class WebFetchConnectorTests
{
    [Fact]
    public async Task ConnectAsync_WhenHostNowResolvesToPrivateAddress_Throws()
    {
        // Arrange: the host passed validation earlier, but resolves to loopback at connect time.
        Task<IPAddress[]> Resolve(string host, CancellationToken ct) => Task.FromResult(new[] { IPAddress.Loopback });

        // Act / Assert
        await Should.ThrowAsync<HttpRequestException>(async () =>
            await WebFetchConnector.ConnectAsync(new DnsEndPoint("example.com", 80), enforce: true, Resolve, CancellationToken.None));
    }

    [Fact]
    public async Task ConnectAsync_WhenAnyResolvedAddressIsPrivate_Throws()
    {
        Task<IPAddress[]> Resolve(string host, CancellationToken ct)
            => Task.FromResult(new[] { IPAddress.Parse("93.184.216.34"), IPAddress.Parse("::ffff:10.0.0.1") });

        await Should.ThrowAsync<HttpRequestException>(async () =>
            await WebFetchConnector.ConnectAsync(new DnsEndPoint("example.com", 80), enforce: true, Resolve, CancellationToken.None));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    [InlineData("::ffff:169.254.169.254")]
    public async Task ConnectAsync_WithPrivateAddressLiteral_ThrowsWithoutResolving(string host)
    {
        var resolved = false;
        Task<IPAddress[]> Resolve(string h, CancellationToken ct)
        {
            resolved = true;
            return Task.FromResult(Array.Empty<IPAddress>());
        }

        await Should.ThrowAsync<HttpRequestException>(async () =>
            await WebFetchConnector.ConnectAsync(new DnsEndPoint(host, 80), enforce: true, Resolve, CancellationToken.None));
        resolved.ShouldBeFalse();
    }

    [Theory]
    [InlineData("GET", "http://example.com/page", null, "proxy.local", 8080, true)]
    [InlineData("CONNECT", "http://proxy.local:8080/", "example.com:443", "proxy.local", 8080, true)]
    [InlineData("GET", "http://example.com/page", null, "example.com", 80, false)]
    [InlineData("GET", "http://example.com/page", null, "proxy.local", 9999, false)]
    [InlineData("GET", "http://example.com:8080/page", null, "10.0.0.1", 8080, false)]
    [InlineData("CONNECT", "http://proxy.local:8080/", null, "proxy.local", 8080, false)]
    public void IsProxyEndpoint_MatchesOnlyTheProxyChosenForTheTarget(
        string method, string requestUri, string? hostHeader, string host, int port, bool expected)
    {
        var proxy = new WebProxy("http://proxy.local:8080");
        var request = new HttpRequestMessage(new HttpMethod(method), requestUri);
        if (hostHeader is not null)
        {
            request.Headers.Host = hostHeader;
        }

        WebFetchConnector.IsProxyEndpoint(proxy, request, new DnsEndPoint(host, port)).ShouldBe(expected);
    }

    [Fact]
    public void IsProxyEndpoint_WithoutProxy_ReturnsFalse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com/");

        WebFetchConnector.IsProxyEndpoint(null, request, new DnsEndPoint("example.com", 80)).ShouldBeFalse();
    }

    [Fact]
    public void IsProxyEndpoint_WhenTargetBypassesProxy_ReturnsFalse()
    {
        var proxy = new WebProxy("http://127.0.0.1:8080", BypassOnLocal: true);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:8080/");

        WebFetchConnector.IsProxyEndpoint(proxy, request, new DnsEndPoint("127.0.0.1", 8080)).ShouldBeFalse();
    }

    [Fact]
    public async Task PrimaryHandler_WithLoopbackProxy_SendsHttpRequestThroughIt()
    {
        using var proxy = new FakeProxy("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok");
        using var client = new HttpClient(WebFetchConnector.CreatePrimaryHandler(new WebProxy(proxy.Uri)));

        var response = await client.GetAsync("http://example.com/page");

        (await response.Content.ReadAsStringAsync()).ShouldBe("ok");
        (await proxy.RequestLine.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe("GET http://example.com/page HTTP/1.1");
    }

    [Fact]
    public async Task PrimaryHandler_WithLoopbackProxy_OpensHttpsTunnelThroughIt()
    {
        // The proxy refuses the tunnel; reaching it at all shows the connection to it was allowed.
        using var proxy = new FakeProxy("HTTP/1.1 403 Forbidden\r\nContent-Length: 0\r\n\r\n");
        using var client = new HttpClient(WebFetchConnector.CreatePrimaryHandler(new WebProxy(proxy.Uri)));

        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync("https://example.com/page"));

        (await proxy.RequestLine.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe("CONNECT example.com:443 HTTP/1.1");
    }

    [Fact]
    public async Task PrimaryHandler_WhenTargetBypassesProxy_StillRefusesPrivateAddress()
    {
        using var proxy = new FakeProxy("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
        using var target = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        target.Start();
        var port = ((IPEndPoint)target.LocalEndpoint).Port;
        var webProxy = new WebProxy(proxy.Uri, BypassOnLocal: true);
        using var client = new HttpClient(WebFetchConnector.CreatePrimaryHandler(webProxy));

        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{port}/"));
        target.Pending().ShouldBeFalse();
    }

    [Fact]
    public async Task PrimaryHandler_RefusesToConnectToLoopbackServer()
    {
        // Arrange: a real server on loopback, reached through the real handler.
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var client = new HttpClient(WebFetchConnector.CreatePrimaryHandler());

        // Act / Assert
        await Should.ThrowAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{port}/"));
        listener.Pending().ShouldBeFalse();
    }

    [Fact]
    public async Task ConnectAsync_WhenNotEnforcing_ConnectsToResolvedAddress()
    {
        // Proxy connections skip the check; this also proves the connect path itself works.
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task<IPAddress[]> Resolve(string host, CancellationToken ct) => Task.FromResult(new[] { IPAddress.Loopback });

        await using var stream = await WebFetchConnector.ConnectAsync(new DnsEndPoint("proxy.local", port), enforce: false, Resolve, CancellationToken.None);

        stream.CanWrite.ShouldBeTrue();
    }

    /// <summary>
    /// A one-shot proxy on loopback that records the first request line and sends a fixed reply.
    /// </summary>
    private sealed class FakeProxy : IDisposable
    {
        private readonly System.Net.Sockets.TcpListener _listener = new(IPAddress.Loopback, 0);

        public FakeProxy(string reply)
        {
            _listener.Start();
            Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}");
            RequestLine = ServeAsync(reply);
        }

        public Uri Uri { get; }

        public Task<string?> RequestLine { get; }

        public void Dispose() => _listener.Stop();

        private async Task<string?> ServeAsync(string reply)
        {
            using var socket = await _listener.AcceptTcpClientAsync();
            var stream = socket.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync();
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
            {
            }

            var bytes = System.Text.Encoding.ASCII.GetBytes(reply);
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            return requestLine;
        }
    }
}
