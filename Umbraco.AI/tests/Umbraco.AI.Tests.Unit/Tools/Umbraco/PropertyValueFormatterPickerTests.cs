using Microsoft.Extensions.DependencyInjection;
using Moq;
using Umbraco.AI.Tests.Unit.Api.Management;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

/// <remarks>
/// The formatter calls the CMS's friendly <c>Url()</c> extension, which reads
/// <see cref="StaticServiceProvider.Instance"/>, so this joins the collection that owns swapping it.
/// </remarks>
[Collection(nameof(StaticServiceProviderTestCollection))]
public class PropertyValueFormatterPickerTests : IDisposable
{
    private readonly IServiceProvider? _originalServiceProvider = StaticServiceProvider.Instance;

    public PropertyValueFormatterPickerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IPublishedUrlProvider>(x => x.GetUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<Uri?>()) == "/url"));
        StaticServiceProvider.Instance = services.BuildServiceProvider();
    }

    public void Dispose() => StaticServiceProvider.Instance = _originalServiceProvider!;

    [Fact]
    public void ExtractProperties_MultiNodeTreePickerOfContent_KeepsTheContentUrl()
    {
        var key = Guid.NewGuid();
        var picked = new Mock<IPublishedContent>();
        picked.SetupGet(x => x.Key).Returns(key);
        picked.SetupGet(x => x.Name).Returns("Home");
        picked.SetupGet(x => x.ContentType).Returns(BuildContentType("page", PublishedItemType.Content));

        var value = Format(CmsConstants.PropertyEditors.Aliases.MultiNodeTreePicker, new[] { picked.Object });

        var item = ((Array)value!).GetValue(0)!;
        item.GetType().GetProperty("key")!.GetValue(item).ShouldBe(key);
        item.GetType().GetProperty("name")!.GetValue(item).ShouldBe("Home");
        item.GetType().GetProperty("url")!.GetValue(item).ShouldBe("/url");
    }

    [Fact]
    public void ExtractProperties_MultiNodeTreePickerOfMembers_FormatsMembersWithoutAUrl()
    {
        var picked = BuildMember("Jane");

        var value = Format(CmsConstants.PropertyEditors.Aliases.MultiNodeTreePicker, new[] { picked });

        var item = ((Array)value!).GetValue(0)!;
        item.GetType().GetProperty("name")!.GetValue(item).ShouldBe("Jane");
        item.GetType().GetProperty("url")!.GetValue(item).ShouldBeNull();
    }

    [Fact]
    public void ExtractProperties_SingleMemberPicker_FormatsTheMemberWithoutAUrl()
    {
        var picked = BuildMember("Jane");

        var value = Format(CmsConstants.PropertyEditors.Aliases.MemberPicker, picked);

        value!.GetType().GetProperty("name")!.GetValue(value).ShouldBe("Jane");
        value.GetType().GetProperty("url")!.GetValue(value).ShouldBeNull();
    }

    [Theory]
    [InlineData(CmsConstants.PropertyEditors.Aliases.MediaPicker3)]
    [InlineData(CmsConstants.PropertyEditors.Aliases.SingleMediaPicker)]
    public void ExtractProperties_MediaPickers_AreFormattedAsMedia(string editorAlias)
    {
        var picked = new Mock<IPublishedContent>();
        picked.SetupGet(x => x.Name).Returns("Hero");
        picked.SetupGet(x => x.ContentType).Returns(BuildContentType("Image", PublishedItemType.Media));

        var value = Format(editorAlias, picked.Object);

        value!.GetType().GetProperty("mediaType")!.GetValue(value).ShouldBe("Image");
        value.GetType().GetProperty("name")!.GetValue(value).ShouldBe("Hero");
    }

    [Fact]
    public void ExtractProperties_MultipleDocumentPicker_KeepsTheContentUrl()
    {
        var picked = new Mock<IPublishedContent>();
        picked.SetupGet(x => x.Key).Returns(Guid.NewGuid());
        picked.SetupGet(x => x.Name).Returns("Home");
        picked.SetupGet(x => x.ContentType).Returns(BuildContentType("page", PublishedItemType.Content));

        var value = Format(CmsConstants.PropertyEditors.Aliases.MultipleDocumentPicker, new[] { picked.Object });

        var item = ((Array)value!).GetValue(0)!;
        item.GetType().GetProperty("name")!.GetValue(item).ShouldBe("Home");
        item.GetType().GetProperty("url")!.GetValue(item).ShouldBe("/url");
    }

    [Fact]
    public void ExtractProperties_MultipleMemberPicker_FormatsMembersWithoutAUrl()
    {
        var value = Format(CmsConstants.PropertyEditors.Aliases.MultipleMemberPicker, new[] { BuildMember("Jane") });

        var item = ((Array)value!).GetValue(0)!;
        item.GetType().GetProperty("name")!.GetValue(item).ShouldBe("Jane");
        item.GetType().GetProperty("url")!.GetValue(item).ShouldBeNull();
    }

    private static IPublishedContent BuildMember(string name)
    {
        var member = new Mock<IPublishedContent>();
        member.SetupGet(x => x.Key).Returns(Guid.NewGuid());
        member.SetupGet(x => x.Name).Returns(name);
        member.SetupGet(x => x.ContentType).Returns(BuildContentType("Member", PublishedItemType.Member));
        return member.Object;
    }

    private static object? Format(string editorAlias, object? propertyValue)
    {
        var dataType = new PublishedDataType(1, editorAlias, editorAlias, new Lazy<object?>(() => null));
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.EditorAlias).Returns(editorAlias);
        propertyType.SetupGet(x => x.DataType).Returns(dataType);

        var property = new Mock<IPublishedProperty>();
        property.SetupGet(x => x.Alias).Returns("p");
        property.SetupGet(x => x.PropertyType).Returns(propertyType.Object);
        property.Setup(x => x.GetValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(propertyValue);

        var content = new Mock<IPublishedContent>();
        content.SetupGet(x => x.Properties).Returns([property.Object]);

        return PropertyValueFormatter.ExtractProperties(content.Object).Single().Value;
    }

    private static IPublishedContentType BuildContentType(string alias, PublishedItemType itemType)
    {
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Alias).Returns(alias);
        contentType.SetupGet(x => x.ItemType).Returns(itemType);
        return contentType.Object;
    }
}
