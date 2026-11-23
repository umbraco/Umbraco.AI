using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Umbraco.AI.Core.Tools.Web;

/// <summary>
/// Builds the primary handler for the web fetch tool's HTTP client and opens its connections.
/// </summary>
/// <remarks>
/// The URL validator resolves the host before the request, but the HTTP client resolves it again when it
/// connects, and the second answer can differ. The connect callback here resolves the host itself, rejects
/// any private, local or reserved address with <see cref="PrivateAddressCheck"/>, and connects only to an
/// address it has checked. Connections to a configured proxy are not checked, and the proxy resolves the target
/// itself, so behind a proxy a changed DNS answer is not caught at connect time.
/// </remarks>
internal static class WebFetchConnector
{
    /// <summary>
    /// Creates the primary handler for the "WebFetchTool" HTTP client.
    /// </summary>
    /// <remarks>
    /// Redirects are not followed here. <see cref="WebContentFetcher"/> follows them itself so each hop is validated.
    /// </remarks>
    public static SocketsHttpHandler CreatePrimaryHandler(IWebProxy? proxy = null)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = true,
            Proxy = proxy,
        };

        handler.ConnectCallback = (context, cancellationToken) =>
        {
            // When the handler goes through a proxy it connects to the proxy, not the target. The proxy comes
            // from the site's own configuration, so it is trusted and not checked here. Behind a proxy the proxy
            // resolves the target itself, so only the URL validator's check applies to the target.
            var activeProxy = handler.UseProxy ? handler.Proxy ?? HttpClient.DefaultProxy : null;
            var enforce = !IsProxyEndpoint(activeProxy, context.InitialRequestMessage, context.DnsEndPoint);
            return ConnectAsync(context.DnsEndPoint, enforce, Dns.GetHostAddressesAsync, cancellationToken);
        };

        return handler;
    }

    /// <summary>
    /// Resolves the endpoint's host and connects to it, refusing private, local or reserved addresses when
    /// <paramref name="enforce"/> is <c>true</c>.
    /// </summary>
    /// <param name="endpoint">The host and port to connect to.</param>
    /// <param name="enforce">Whether to reject private, local or reserved addresses.</param>
    /// <param name="resolve">Resolves a host name to its addresses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal static async ValueTask<Stream> ConnectAsync(
        DnsEndPoint endpoint,
        bool enforce,
        Func<string, CancellationToken, Task<IPAddress[]>> resolve,
        CancellationToken cancellationToken)
    {
        var host = TrimBrackets(endpoint.Host);
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await resolve(host, cancellationToken);

        if (addresses.Length == 0)
        {
            throw new SocketException((int)SocketError.HostNotFound);
        }

        if (enforce && addresses.Any(PrivateAddressCheck.IsBlocked))
        {
            throw new HttpRequestException($"Cannot access private or local IP address for host {host}");
        }

        Exception? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                socket.Dispose();
                lastError = ex;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw lastError!;
    }

    /// <summary>
    /// Returns <c>true</c> only when <paramref name="endpoint"/> is the proxy that <paramref name="proxy"/> chooses
    /// for the request's target. Every other connection is treated as a direct connection to the target.
    /// </summary>
    /// <param name="proxy">The proxy the handler uses, or <c>null</c> when it uses none.</param>
    /// <param name="request">The request that opened the connection. For HTTPS through a proxy this is the
    /// CONNECT request, whose Host header names the target.</param>
    /// <param name="endpoint">The endpoint the handler is connecting to.</param>
    internal static bool IsProxyEndpoint(IWebProxy? proxy, HttpRequestMessage request, DnsEndPoint endpoint)
    {
        if (proxy is null || request.RequestUri is null)
        {
            return false;
        }

        var target = request.RequestUri;
        if (request.Method == HttpMethod.Connect)
        {
            // The CONNECT request is addressed to the proxy; its Host header carries the target authority.
            if (string.IsNullOrEmpty(request.Headers.Host)
                || !Uri.TryCreate($"https://{request.Headers.Host}/", UriKind.Absolute, out target))
            {
                return false;
            }
        }

        if (proxy.IsBypassed(target))
        {
            return false;
        }

        var proxyUri = proxy.GetProxy(target);
        if (proxyUri is null || !proxyUri.IsAbsoluteUri)
        {
            return false;
        }

        return string.Equals(
                   TrimBrackets(proxyUri.IdnHost),
                   TrimBrackets(endpoint.Host),
                   StringComparison.OrdinalIgnoreCase)
               && proxyUri.Port == endpoint.Port;
    }

    private static string TrimBrackets(string host)
        => host.Length > 1 && host[0] == '[' && host[^1] == ']' ? host[1..^1] : host;
}
