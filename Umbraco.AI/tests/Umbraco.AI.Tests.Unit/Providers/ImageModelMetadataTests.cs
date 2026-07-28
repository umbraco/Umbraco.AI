using Umbraco.AI.Core.Models;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Tests.Unit.Providers;

/// <summary>
/// Reading the per-model image constraints a capability declares.
/// </summary>
/// <remarks>
/// These keys shipped with image generation and had no reader at all: providers wrote them, the backoffice
/// received them, and nothing consumed them — so a size the model rejects saved cleanly and failed only at
/// generation time. The readers exist so there is one place that knows the format, and these tests pin what
/// an absent or malformed declaration means.
/// </remarks>
public class ImageModelMetadataTests
{
    [Fact]
    public void GetSupportedImageSizes_Declared_ReturnsThemInOrder()
    {
        var model = Describe(new() { [AIModelMetadataKeys.ImageSupportedSizes] = "1024x1024,1024x1536,1536x1024" });

        model.GetSupportedImageSizes().ShouldBe(["1024x1024", "1024x1536", "1536x1024"]);
    }

    [Fact]
    public void GetSupportedImageSizes_Whitespace_IsTrimmed()
    {
        var model = Describe(new() { [AIModelMetadataKeys.ImageSupportedSizes] = " 1024x1024 , 512x512 ,, " });

        model.GetSupportedImageSizes().ShouldBe(["1024x1024", "512x512"]);
    }

    [Fact]
    public void GetSupportedImageSizes_NotDeclared_ReturnsEmpty()
    {
        // Empty means "unknown", not "none supported" — the editor keeps accepting a typed size, rather than
        // restricting every model a provider happens not to describe to an empty dropdown.
        Describe(new()).GetSupportedImageSizes().ShouldBeEmpty();
    }

    [Fact]
    public void GetImageMaxEdge_Declared_IsParsed()
    {
        Describe(new() { [AIModelMetadataKeys.ImageMaxEdge] = "1536" }).GetImageMaxEdge().ShouldBe(1536);
    }

    [Theory]
    [InlineData("")]
    [InlineData("wide")]
    public void GetImageMaxEdge_Unparseable_ReturnsNull(string value)
    {
        // Metadata is a string dictionary, so a provider can put anything in it. Reading it as null beats
        // throwing on a model list the user only wanted to browse.
        Describe(new() { [AIModelMetadataKeys.ImageMaxEdge] = value }).GetImageMaxEdge().ShouldBeNull();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    public void SupportsImageEdit_Declared_IsParsed(string value, bool expected)
    {
        Describe(new() { [AIModelMetadataKeys.ImageSupportsEdit] = value }).SupportsImageEdit().ShouldBe(expected);
    }

    [Fact]
    public void SupportsImageEdit_NotDeclared_ReturnsNull()
    {
        // Null rather than false: silence is not a denial, and a consumer that conflates them hides the
        // feature on every model nobody has described yet.
        Describe(new()).SupportsImageEdit().ShouldBeNull();
    }

    [Fact]
    public void SupportsImageMask_ReadsItsOwnKey()
    {
        var model = Describe(new()
        {
            [AIModelMetadataKeys.ImageSupportsEdit] = "true",
            [AIModelMetadataKeys.ImageSupportsMask] = "false",
        });

        model.SupportsImageEdit().ShouldBe(true);
        model.SupportsImageMask().ShouldBe(false);
    }

    private static AIModelDescriptor Describe(Dictionary<string, string> metadata)
        => new(new AIModelRef("test", "some-image-model"), "Some Image Model", metadata);
}
