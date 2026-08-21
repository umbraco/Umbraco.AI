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

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class CreateUmbracoContentToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly Mock<IContentTypeService> _contentTypeServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public CreateUmbracoContentToolTests()
    {
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _contentTypeServiceMock = new Mock<IContentTypeService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new CreateUmbracoContentTool(
            _contentEditingServiceMock.Object,
            _contentTypeServiceMock.Object,
            _authorizerMock.Object);
    }

    private static Mock<IContent> CreateContentMock(Guid key, string name, string contentTypeAlias)
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
        contentMock.Setup(x => x.Properties).Returns(new PropertyCollection());
        return contentMock;
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyContentTypeAlias_ReturnsError()
    {
        var args = new CreateUmbracoContentArgs(null, "", "Home", null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyName_ReturnsError()
    {
        var args = new CreateUmbracoContentArgs(null, "blogPost", "", null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingServices()
    {
        var args = new CreateUmbracoContentArgs(Guid.NewGuid(), "blogPost", "Home", null);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionNew.ActionLetter, args.ParentKey, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _contentTypeServiceMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);
        _contentEditingServiceMock.Verify(
            x => x.CreateAsync(It.IsAny<ContentCreateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ContentTypeNotFound_ReturnsError()
    {
        var userKey = Guid.NewGuid();
        var args = new CreateUmbracoContentArgs(null, "missingType", "Home", null);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionNew.ActionLetter, null, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentTypeServiceMock.Setup(x => x.Get("missingType")).Returns((IContentType?)null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_CreateFails_ReturnsMappedMessage()
    {
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        var args = new CreateUmbracoContentArgs(null, "blogPost", "Home", null);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionNew.ActionLetter, null, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        var contentTypeMock = new Mock<IContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeKey);
        _contentTypeServiceMock.Setup(x => x.Get("blogPost")).Returns(contentTypeMock.Object);
        _contentEditingServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<ContentCreateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentCreateResult, ContentEditingOperationStatus>.Fail(
                ContentEditingOperationStatus.PropertyValidationError, new ContentCreateResult()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("validation");
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_CreatesWithResolvedUserKeyAndReturnsContent()
    {
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        var parentKey = Guid.NewGuid();
        var newKey = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement>
        {
            ["title"] = JsonDocument.Parse("\"Hello\"").RootElement,
        };
        var args = new CreateUmbracoContentArgs(parentKey, "blogPost", "Home", propertyValues, "en-US");

        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionNew.ActionLetter, parentKey, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var contentTypeMock = new Mock<IContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeKey);
        _contentTypeServiceMock.Setup(x => x.Get("blogPost")).Returns(contentTypeMock.Object);

        var createdContentMock = CreateContentMock(newKey, "Home", "blogPost");

        ContentCreateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<ContentCreateModel>(), userKey))
            .Callback<ContentCreateModel, Guid>((model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentCreateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentCreateResult { Content = createdContentMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
        typed.Content.ShouldNotBeNull();
        typed.Content!.Key.ShouldBe(newKey);

        capturedModel.ShouldNotBeNull();
        capturedModel!.ContentTypeKey.ShouldBe(contentTypeKey);
        capturedModel.ParentKey.ShouldBe(parentKey);
        capturedModel.Variants.Single().Name.ShouldBe("Home");
        capturedModel.Variants.Single().Culture.ShouldBe("en-US");
        capturedModel.Properties.Single().Alias.ShouldBe("title");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("content type");
    }

    [Fact]
    public async Task DescribeInvocationAsync_NoParentKey_DescribesCreatingAtTheRoot()
    {
        var args = new CreateUmbracoContentArgs(null, "blogPost", "Home", null);

        var description = await _tool.DescribeInvocationAsync(args);

        description.ShouldBe("Create a new 'blogPost' content item named 'Home' at the root.");
    }

    [Fact]
    public async Task DescribeInvocationAsync_ParentResolves_UsesItsNameInsteadOfTheRawKey()
    {
        var parentKey = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(parentKey)).ReturnsAsync(Mock.Of<IContent>(c => c.Name == "Blog"));
        var args = new CreateUmbracoContentArgs(parentKey, "blogPost", "Home", null);

        var description = await _tool.DescribeInvocationAsync(args);

        description.ShouldBe("Create a new 'blogPost' content item named 'Home' under parent 'Blog'.");
    }

    [Fact]
    public async Task DescribeInvocationAsync_ParentDoesNotResolve_FallsBackToTheRawKey()
    {
        var parentKey = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(parentKey)).ReturnsAsync((IContent?)null);
        var args = new CreateUmbracoContentArgs(parentKey, "blogPost", "Home", null);

        var description = await _tool.DescribeInvocationAsync(args);

        description.ShouldBe($"Create a new 'blogPost' content item named 'Home' under parent {parentKey}.");
    }
}
