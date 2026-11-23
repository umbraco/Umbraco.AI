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
    [InlineData("http://example.com/page", "example.com", 80, false)]
    [InlineData("https://EXAMPLE.com/page", "example.com", 443, false)]
    [InlineData("http://[2606:4700::1]/", "[2606:4700::1]", 80, false)]
    [InlineData("http://[2606:4700::1]/", "2606:4700::1", 80, false)]
    [InlineData("https://example.com/page", "proxy.internal", 8080, true)]
    [InlineData("https://example.com/page", "example.com", 8080, true)]
    public void IsProxyConnection_ComparesEndpointWithRequestHost(string requestUri, string host, int port, bool expected)
    {
        WebFetchConnector.IsProxyConnection(new Uri(requestUri), new DnsEndPoint(host, port)).ShouldBe(expected);
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
}
