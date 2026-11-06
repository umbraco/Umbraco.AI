using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class InvariantForVariantGuardTests
{
    private static IPropertyType PropertyType(string alias, ContentVariation variations)
    {
        var mock = new Mock<IPropertyType>();
        mock.Setup(x => x.Alias).Returns(alias);
        mock.Setup(x => x.Variations).Returns(variations);
        return mock.Object;
    }

    private static readonly IPropertyType[] PropertyTypes =
    [
        PropertyType("shared", ContentVariation.Nothing),
        PropertyType("perCulture", ContentVariation.Culture),
    ];

    private static UmbracoWriteAuthorizationResult Auth(bool hasAccess) =>
        UmbracoWriteAuthorizationResult.Allowed(Guid.NewGuid(), hasAccess);

    [Fact]
    public void GetDenialMessage_SharedPropertyOnVariantContent_WithoutPermission_ReturnsClearMessage()
    {
        var message = InvariantForVariantGuard.GetDenialMessage(Auth(false), true, PropertyTypes, ["shared"]);

        message.ShouldNotBeNull();
        message.ShouldContain("'shared'");
        message.ShouldContain("invariant-for-variant permission");
    }

    [Fact]
    public void GetDenialMessage_SharedPropertyOnVariantContent_WithPermission_ReturnsNull()
    {
        InvariantForVariantGuard.GetDenialMessage(Auth(true), true, PropertyTypes, ["shared"]).ShouldBeNull();
    }

    [Fact]
    public void GetDenialMessage_InvariantContent_IsUnaffectedByPermission()
    {
        InvariantForVariantGuard.GetDenialMessage(Auth(false), false, PropertyTypes, ["shared"]).ShouldBeNull();
    }

    [Fact]
    public void GetDenialMessage_OnlyCultureVariantProperties_WithoutPermission_ReturnsNull()
    {
        InvariantForVariantGuard.GetDenialMessage(Auth(false), true, PropertyTypes, ["perCulture"]).ShouldBeNull();
    }

    [Fact]
    public void GetDenialMessage_MixedProperties_NamesOnlyTheSharedOnes()
    {
        var message = InvariantForVariantGuard.GetDenialMessage(Auth(false), true, PropertyTypes, ["perCulture", "shared"]);

        message.ShouldNotBeNull();
        message.ShouldContain("'shared'");
        message.ShouldNotContain("'perCulture'");
    }
}
