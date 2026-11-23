using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Umbraco.AI.Core.Tools.Web;

/// <summary>
/// Decides whether an IP address is private, local or reserved, and so must never be fetched by the web fetch tool.
/// </summary>
/// <remarks>
/// Used both when a URL is validated and again when the HTTP client opens a connection, so both checks agree.
/// IPv6 forms that carry an IPv4 address (mapped, compatible, NAT64 and 6to4) are judged by the IPv4 address
/// they carry.
/// </remarks>
internal static class PrivateAddressCheck
{
    // IPv4 ranges that are not public unicast (network, prefix length).
    private static readonly (uint Network, int Prefix)[] BlockedIPv4Ranges =
    [
        (0x00000000, 8),  // 0.0.0.0/8       "this network"
        (0x0A000000, 8),  // 10.0.0.0/8      private
        (0x64400000, 10), // 100.64.0.0/10   carrier-grade NAT
        (0x7F000000, 8),  // 127.0.0.0/8     loopback
        (0xA9FE0000, 16), // 169.254.0.0/16  link-local (includes cloud instance metadata)
        (0xAC100000, 12), // 172.16.0.0/12   private
        (0xC0000000, 24), // 192.0.0.0/24    IETF protocol assignments
        (0xC0000200, 24), // 192.0.2.0/24    documentation
        (0xC0A80000, 16), // 192.168.0.0/16  private
        (0xC6120000, 15), // 198.18.0.0/15   benchmarking
        (0xC6336400, 24), // 198.51.100.0/24 documentation
        (0xCB007100, 24), // 203.0.113.0/24  documentation
        (0xE0000000, 4),  // 224.0.0.0/4     multicast
        (0xF0000000, 4),  // 240.0.0.0/4     reserved, includes 255.255.255.255 broadcast
    ];

    /// <summary>
    /// Returns <c>true</c> when the address is private, local or reserved and must not be connected to.
    /// </summary>
    /// <param name="address">The address to check.</param>
    public static bool IsBlocked(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsBlockedIPv4(address),
            AddressFamily.InterNetworkV6 => IsBlockedIPv6(address),
            _ => true, // Anything that is not IPv4 or IPv6 is never a valid fetch target.
        };
    }

    private static bool IsBlockedIPv4(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[4];
        address.TryWriteBytes(bytes, out _);
        return IsBlockedIPv4(BinaryPrimitives.ReadUInt32BigEndian(bytes));
    }

    private static bool IsBlockedIPv4(uint value)
    {
        foreach (var (network, prefix) in BlockedIPv4Ranges)
        {
            var mask = uint.MaxValue << (32 - prefix);
            if ((value & mask) == network)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlockedIPv6(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[16];
        address.TryWriteBytes(bytes, out _);

        // ::/96 covers the unspecified address (::), loopback (::1) and deprecated IPv4-compatible
        // addresses (::a.b.c.d). Judge them all by the IPv4 address in the last four bytes.
        if (bytes[..12].IndexOfAnyExcept((byte)0) < 0)
        {
            return IsBlockedIPv4(BinaryPrimitives.ReadUInt32BigEndian(bytes[12..]));
        }

        // fc00::/7 unique local, fe80::/10 link-local, fec0::/10 site-local, ff00::/8 multicast.
        if ((bytes[0] & 0xFE) == 0xFC
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6Multicast)
        {
            return true;
        }

        var first16 = BinaryPrimitives.ReadUInt16BigEndian(bytes);
        var second16 = BinaryPrimitives.ReadUInt16BigEndian(bytes[2..]);

        // 64:ff9b::/96 well-known NAT64 prefix: judge by the embedded IPv4 address.
        if (first16 == 0x0064 && second16 == 0xFF9B && bytes[4..12].IndexOfAnyExcept((byte)0) < 0)
        {
            return IsBlockedIPv4(BinaryPrimitives.ReadUInt32BigEndian(bytes[12..]));
        }

        // 64:ff9b:1::/48 local-use NAT64 prefix: only reachable inside the local network.
        if (first16 == 0x0064 && second16 == 0xFF9B && BinaryPrimitives.ReadUInt16BigEndian(bytes[4..]) == 0x0001)
        {
            return true;
        }

        // 2002::/16 6to4: judge by the IPv4 address in bytes 2-5.
        if (first16 == 0x2002)
        {
            return IsBlockedIPv4(BinaryPrimitives.ReadUInt32BigEndian(bytes[2..]));
        }

        // 100::/64 discard-only.
        if (first16 == 0x0100 && bytes[2..8].IndexOfAnyExcept((byte)0) < 0)
        {
            return true;
        }

        // 2001:db8::/32 documentation.
        if (first16 == 0x2001 && second16 == 0x0DB8)
        {
            return true;
        }

        return false;
    }
}
