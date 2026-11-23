using Microsoft.Extensions.Options;

using Umbraco.AI.Core.Tools.Web;

namespace Umbraco.AI.Tests.Unit.Tools.Web;

public class UrlValidatorTests
{
    private readonly UrlValidator _validator = new(Options.Create(new AIWebFetchOptions()));

    // IP literals resolve without a DNS lookup, so these run offline.
    [Theory]
    [InlineData("http://0.0.0.0/")]
    [InlineData("http://100.64.0.1/")]
    [InlineData("http://192.0.0.8/")]
    [InlineData("http://198.18.0.1/")]
    [InlineData("http://240.0.0.1/")]
    [InlineData("http://255.255.255.255/")]
    [InlineData("http://169.254.169.254/latest/")]
    [InlineData("http://[::]/")]
    [InlineData("http://[::1]/")]
    [InlineData("http://[::ffff:127.0.0.1]/")]
    [InlineData("http://[::ffff:a9fe:a9fe]/")]
    [InlineData("http://[64:ff9b::7f00:1]/")]
    [InlineData("http://[2002:a00:1::]/")]
    public async Task ValidateAsync_WithPrivateOrReservedAddress_IsInvalid(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("http://93.184.216.34/")]
    [InlineData("https://[2606:4700:4700::1111]/")]
    public async Task ValidateAsync_WithPublicAddress_IsValid(string url)
    {
        var result = await _validator.ValidateAsync(url);

        result.IsValid.ShouldBeTrue();
        result.NormalizedUrl.ShouldNotBeNull();
    }
}
