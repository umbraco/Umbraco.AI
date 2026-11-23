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
/// address it has checked.
/// </remarks>
internal static class WebFetchConnector
{
    /// <summary>
    /// Creates the primary handler for the "WebFetchTool" HTTP client.
    /// </summary>
    /// <remarks>
    /// Redirects are not followed here. <see cref="WebContentFetcher"/> follows them itself so each hop is validated.
    /// </remarks>
    public static SocketsHttpHandler CreatePrimaryHandler() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = true,
        ConnectCallback = ConnectAsync,
    };

    private static ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        // With a proxy configured, the handler connects to the proxy rather than the target. The proxy is set by
        // the site's own configuration, so it is trusted; the target was already checked by the URL validator.
        var enforce = !IsProxyConnection(context.InitialRequestMessage.RequestUri, context.DnsEndPoint);
        return ConnectAsync(context.DnsEndPoint, enforce, Dns.GetHostAddressesAsync, cancellationToken);
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
    /// Returns <c>true</c> when the handler is connecting to somewhere other than the request's own host,
    /// which only happens when it connects to a proxy.
    /// </summary>
    /// <param name="requestUri">The request's URI.</param>
    /// <param name="endpoint">The endpoint the handler is connecting to.</param>
    internal static bool IsProxyConnection(Uri? requestUri, DnsEndPoint endpoint)
    {
        if (requestUri is null)
        {
            return false;
        }

        return !string.Equals(
                   TrimBrackets(requestUri.IdnHost),
                   TrimBrackets(endpoint.Host),
                   StringComparison.OrdinalIgnoreCase)
               || requestUri.Port != endpoint.Port;
    }

    private static string TrimBrackets(string host)
        => host.Length > 1 && host[0] == '[' && host[^1] == ']' ? host[1..^1] : host;
}
