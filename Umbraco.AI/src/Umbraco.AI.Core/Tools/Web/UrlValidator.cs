using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Umbraco.AI.Core.Tools.Web;

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

    private readonly AIWebFetchOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlValidator"/> class.
    /// </summary>
    /// <param name="options">Web fetch options.</param>
    public UrlValidator(IOptions<AIWebFetchOptions> options)
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

        // Check protocol safelist
        if (!AllowedSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            return new UrlValidationResult(false, $"Protocol '{uri.Scheme}' is not allowed. Only HTTP and HTTPS are supported", null);

        // Normalize URL
        var normalizedUrl = uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);

        // Check blocked hosts (localhost, metadata endpoints)
        var host = uri.Host.ToLowerInvariant();
        if (PrivateHostnames.Contains(host))
            return new UrlValidationResult(false, $"Cannot access {host}", null);

        // Check domain blocklist
        if (_options.BlockedDomains.Count > 0)
        {
            if (_options.BlockedDomains.Any(blocked =>
                host.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{blocked}", StringComparison.OrdinalIgnoreCase)))
            {
                return new UrlValidationResult(false, $"Domain {host} is blocked", null);
            }
        }

        // Check domain safelist (if configured, only allow these domains)
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
            // DnsSafeHost drops the brackets around an IPv6 literal so it parses as an address.
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
            if (addresses.Length == 0)
                return new UrlValidationResult(false, $"Could not resolve hostname: {host}", null);

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
    /// Checks if an IP address is private, local or reserved.
    /// </summary>
    private static bool IsPrivateOrLocalIpAddress(IPAddress address)
        => PrivateAddressCheck.IsBlocked(address);
}
