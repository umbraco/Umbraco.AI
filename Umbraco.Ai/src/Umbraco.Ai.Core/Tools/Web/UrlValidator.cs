using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Umbraco.Ai.Core.Tools.Web;

/// <summary>
/// Validates URLs for security concerns to prevent SSRF attacks.
/// </summary>
public class UrlValidator : IUrlValidator
{
    private static readonly string[] AllowedSchemes = { "http", "https" };
    private static readonly int MaxUrlLength = 2048;

    // Private IP ranges (RFC 1918 and others)
    private static readonly string[] PrivateHostnames = {
        "localhost",
        "metadata.google.internal" // GCP metadata
    };

    private readonly AiWebFetchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlValidator"/> class.
    /// </summary>
    /// <param name="options">Web fetch options.</param>
    public UrlValidator(IOptions<AiWebFetchOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<UrlValidationResult> ValidateAsync(string url, CancellationToken cancellationToken = default)
    {
        // Check if null or empty
        if (string.IsNullOrWhiteSpace(url))
            return new UrlValidationResult(false, "URL cannot be empty", null);

        // Check URL length
        if (url.Length > MaxUrlLength)
            return new UrlValidationResult(false, $"URL exceeds maximum length of {MaxUrlLength} characters", null);

        // Parse URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return new UrlValidationResult(false, "Invalid URL format", null);

        // Check protocol whitelist
        if (!AllowedSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            return new UrlValidationResult(false, $"Protocol '{uri.Scheme}' is not allowed. Only HTTP and HTTPS are supported", null);

        // Normalize URL
        var normalizedUrl = uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);

        // Check blocked hosts (localhost, metadata endpoints)
        var host = uri.Host.ToLowerInvariant();
        if (PrivateHostnames.Contains(host))
            return new UrlValidationResult(false, $"Cannot access {host}", null);

        // Check domain blacklist
        if (_options.BlockedDomains.Count > 0)
        {
            if (_options.BlockedDomains.Any(blocked =>
                host.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{blocked}", StringComparison.OrdinalIgnoreCase)))
            {
                return new UrlValidationResult(false, $"Domain {host} is blocked", null);
            }
        }

        // Check domain whitelist (if configured, only allow these domains)
        if (_options.AllowedDomains.Count > 0)
        {
            var isAllowed = _options.AllowedDomains.Any(allowed =>
                host.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{allowed}", StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
                return new UrlValidationResult(false, $"Domain {host} is not in the allowed list", null);
        }

        // Resolve DNS and validate IP addresses
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);

            foreach (var address in addresses)
            {
                if (IsPrivateOrLocalIpAddress(address))
                    return new UrlValidationResult(false, $"Cannot access private or local IP address: {address}", null);

                // Block AWS/Azure metadata endpoint
                if (address.ToString() == "169.254.169.254")
                    return new UrlValidationResult(false, "Cannot access cloud metadata endpoint", null);
            }
        }
        catch (SocketException)
        {
            return new UrlValidationResult(false, $"Could not resolve hostname: {host}", null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UrlValidationResult(false, $"DNS resolution failed: {ex.Message}", null);
        }

        return new UrlValidationResult(true, null, normalizedUrl);
    }

    /// <summary>
    /// Checks if an IP address is private or local.
    /// </summary>
    private static bool IsPrivateOrLocalIpAddress(IPAddress address)
    {
        // Loopback addresses
        if (IPAddress.IsLoopback(address))
            return true;

        // Link-local addresses
        if (address.IsIPv6LinkLocal)
            return true;

        // IPv6 unique local addresses (fc00::/7)
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC) // fc00::/7
                return true;
        }

        // IPv4 private ranges
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // 169.254.0.0/16 (link-local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
        }

        return false;
    }
}
