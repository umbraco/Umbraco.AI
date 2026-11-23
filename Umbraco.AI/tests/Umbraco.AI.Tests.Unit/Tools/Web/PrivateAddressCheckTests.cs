using System.Net;

using Umbraco.AI.Core.Tools.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Web;

public class PrivateAddressCheckTests
{
    [Theory]
    // IPv4 private, local and reserved ranges
    [InlineData("0.0.0.0")]
    [InlineData("0.1.2.3")]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("100.127.255.254")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.1")]
    [InlineData("192.88.99.1")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.254")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.250")]
    [InlineData("240.0.0.1")]
    [InlineData("255.255.255.255")]
    // IPv6 local and reserved ranges
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("fe80::1")]
    [InlineData("fec0::1")]
    [InlineData("ff02::1")]
    [InlineData("100::1")]
    [InlineData("2001:db8::1")]
    [InlineData("64:ff9b:1::1")]
    [InlineData("2001::1")]
    [InlineData("2001:0:4136:e378:8000:63bf:3fff:fdd2")]
    [InlineData("2001:1ff::1")]
    [InlineData("::ffff:0:808:808")]
    // IPv6 forms carrying a private IPv4 address
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::127.0.0.1")]
    [InlineData("::10.0.0.1")]
    [InlineData("64:ff9b::127.0.0.1")]
    [InlineData("64:ff9b::a9fe:a9fe")]
    [InlineData("2002:7f00:0001::")]
    [InlineData("2002:c0a8:0101::1")]
    public void IsBlocked_WithPrivateLocalOrReservedAddress_ReturnsTrue(string address)
    {
        PrivateAddressCheck.IsBlocked(IPAddress.Parse(address)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("198.17.255.255")]
    [InlineData("198.20.0.1")]
    [InlineData("223.255.255.255")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::8.8.8.8")]
    [InlineData("2002:0808:0808::1")]
    [InlineData("2001:200::1")]
    [InlineData("2001:4860:4860::8888")]
    public void IsBlocked_WithPublicAddress_ReturnsFalse(string address)
    {
        PrivateAddressCheck.IsBlocked(IPAddress.Parse(address)).ShouldBeFalse();
    }
}
