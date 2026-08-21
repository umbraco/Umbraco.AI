using System.Text.Json;

using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Extensions;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class UpdateUmbracoContentToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public UpdateUmbracoContentToolTests()
    {
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new UpdateUmbracoContentTool(_contentEditingServiceMock.Object, _authorizerMock.Object);
    }

    private static Mock<IContent> CreateContentMock(Guid key, string name, string contentTypeAlias, IEnumerable<IProperty>? properties = null)
    {
        var contentTypeMock = new Mock<ISimpleContentType>();
        contentTypeMock.Setup(x => x.Alias).Returns(contentTypeAlias);

        var contentMock = new Mock<IContent>();
        contentMock.Setup(x => x.Key).Returns(key);
        contentMock.Setup(x => x.Name).Returns(name);
        contentMock.Setup(x => x.GetCultureName(It.IsAny<string?>())).Returns((string?)null);
        contentMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);
        contentMock.Setup(x => x.CreateDate).Returns(DateTime.UtcNow);
        contentMock.Setup(x => x.UpdateDate).Returns(DateTime.UtcNow);
        contentMock.Setup(x => x.Level).Returns(1);
        contentMock.Setup(x => x.Path).Returns("-1,1234");
        contentMock.Setup(x => x.Properties).Returns(new PropertyCollection(properties ?? []));
        return contentMock;
    }

    private static Mock<IProperty> CreatePropertyMock(string alias, object? value, bool variesByCulture = false, bool variesBySegment = false)
    {
        var variations = ContentVariation.Nothing;
        if (variesByCulture)
        {
            variations |= ContentVariation.Culture;
        }

        if (variesBySegment)
        {
            variations |= ContentVariation.Segment;
        }

        // VariesByCulture()/VariesBySegment() are extension methods over Variations, not overridable
        // interface members, so Moq can't Setup() them directly — the underlying Variations property is
        // the real, mockable seam.
        var propertyTypeMock = new Mock<IPropertyType>();
        propertyTypeMock.Setup(x => x.Alias).Returns(alias);
        propertyTypeMock.Setup(x => x.Variations).Returns(variations);

        var propertyMock = new Mock<IProperty>();
        propertyMock.Setup(x => x.Alias).Returns(alias);
        propertyMock.Setup(x => x.PropertyType).Returns(propertyTypeMock.Object);
        propertyMock.Setup(x => x.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(value);
        return propertyMock;
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new UpdateUmbracoContentArgs(Guid.Empty, "New Name", null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new UpdateUmbracoContentArgs(key, "New Name", null);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _contentEditingServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ContentUpdateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateFails_ReturnsMappedMessage()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        // A name is supplied here so the call reaches UpdateAsync directly — this test targets the
        // ToMessage() mapping on a failed UpdateAsync, not the no-name current-name lookup (covered below).
        var args = new UpdateUmbracoContentArgs(key, "Some Name", null);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(key, "Some Name", "blogPost").Object);
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Fail(
                ContentEditingOperationStatus.NotFound, new ContentUpdateResult()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_UpdatesWithResolvedUserKeyAndReturnsContent()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement>
        {
            ["summary"] = JsonDocument.Parse("\"Updated\"").RootElement,
        };
        var args = new UpdateUmbracoContentArgs(key, "New Name", propertyValues, "en-US");

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(key, "Existing Name", "blogPost").Object);

        var updatedContentMock = CreateContentMock(key, "New Name", "blogPost");

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult { Content = updatedContentMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
        typed.Content.ShouldNotBeNull();
        typed.Content!.Key.ShouldBe(key);

        capturedModel.ShouldNotBeNull();
        capturedModel!.Variants.Single().Name.ShouldBe("New Name");
        capturedModel.Variants.Single().Culture.ShouldBe("en-US");
        capturedModel.Properties.Single().Alias.ShouldBe("summary");
    }

    [Fact]
    public async Task ExecuteAsync_PropertyValuesOmitsExistingProperty_ResubmitsItsCurrentValueSoItSurvives()
    {
        // ContentEditingServiceBase.RemoveMissingProperties wipes any property alias not present in the
        // submitted set on every save. update_umbraco_content is documented as a patch — only the aliases
        // named in PropertyValues should change — so an untouched property like "bodyText" here must come
        // back out in the captured model with its current value, not be silently dropped.
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement>
        {
            ["summary"] = JsonDocument.Parse("\"Updated Summary\"").RootElement,
        };
        var args = new UpdateUmbracoContentArgs(key, null, propertyValues);

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var existingProperties = new List<IProperty>
        {
            CreatePropertyMock("summary", "Old Summary").Object,
            CreatePropertyMock("bodyText", "Old Body").Object,
        };
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(key, "Existing Name", "blogPost", existingProperties).Object);

        ContentUpdateModel? capturedModel = null;
        var updatedContentMock = CreateContentMock(key, "Existing Name", "blogPost", existingProperties);
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult { Content = updatedContentMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        result.ShouldBeOfType<UpdateUmbracoContentResult>().Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.Properties.Count().ShouldBe(2);

        var summary = capturedModel.Properties.Single(p => p.Alias == "summary");
        summary.Value.ShouldBeOfType<JsonElement>();
        ((JsonElement)summary.Value!).GetString().ShouldBe("Updated Summary");

        var bodyText = capturedModel.Properties.Single(p => p.Alias == "bodyText");
        bodyText.Value.ShouldBeOfType<JsonElement>();
        ((JsonElement)bodyText.Value!).GetString().ShouldBe("Old Body");
    }

    [Fact]
    public async Task ExecuteAsync_UntouchedPropertyVariesBySegment_IsLeftOutOfThePatch()
    {
        // This tool has no Segment argument, so a segment-varying property can't be safely read back and
        // resubmitted (which segment would we even ask for?). It's left out of the patch fill-in rather
        // than risk a NotSupportedException from guessing — the pre-existing removed-if-omitted behavior
        // for this one, narrow, unsupported case.
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement>
        {
            ["summary"] = JsonDocument.Parse("\"Updated Summary\"").RootElement,
        };
        var args = new UpdateUmbracoContentArgs(key, null, propertyValues);

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var existingProperties = new List<IProperty>
        {
            CreatePropertyMock("summary", "Old Summary").Object,
            CreatePropertyMock("regionalNote", "Old Note", variesBySegment: true).Object,
        };
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(key, "Existing Name", "blogPost", existingProperties).Object);

        ContentUpdateModel? capturedModel = null;
        var updatedContentMock = CreateContentMock(key, "Existing Name", "blogPost", existingProperties);
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult { Content = updatedContentMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        result.ShouldBeOfType<UpdateUmbracoContentResult>().Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.Properties.Select(p => p.Alias).ShouldBe(new[] { "summary" });
    }

    [Fact]
    public async Task ExecuteAsync_NoNameChange_LoadsCurrentNameToAvoidCultureVarianceMismatch()
    {
        // ContentEditingServiceBase requires a Variants entry matching the content type's variance
        // regardless of what's being changed — an invariant type with an empty Variants list fails with
        // ContentTypeCultureVarianceMismatch even though nothing here is renaming the item. When no new
        // name is given, the tool must load the current one so a matching entry is still present.
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement>
        {
            ["summary"] = JsonDocument.Parse("\"Updated\"").RootElement,
        };
        var args = new UpdateUmbracoContentArgs(key, null, propertyValues);

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(key, "Existing Name", "blogPost").Object);

        ContentUpdateModel? capturedModel = null;
        var updatedContentMock = CreateContentMock(key, "Existing Name", "blogPost");
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult { Content = updatedContentMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        var variant = capturedModel!.Variants.Single();
        variant.Name.ShouldBe("Existing Name");
        variant.Culture.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_NoNameChange_ContentNotFound_ReturnsErrorWithoutCallingUpdate()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new UpdateUmbracoContentArgs(key, null, null);

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IContent?)null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
        _contentEditingServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ContentUpdateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("draft");
    }
}
