using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class UnpublishUmbracoContentToolTests
{
    private readonly Mock<IContentPublishingService> _contentPublishingServiceMock;
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public UnpublishUmbracoContentToolTests()
    {
        _contentPublishingServiceMock = new Mock<IContentPublishingService>();
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new UnpublishUmbracoContentTool(_contentPublishingServiceMock.Object, _contentEditingServiceMock.Object, _authorizerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new UnpublishUmbracoContentArgs(Guid.Empty);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UnpublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new UnpublishUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUnpublish.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UnpublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _contentPublishingServiceMock.Verify(
            x => x.UnpublishAsync(It.IsAny<Guid>(), It.IsAny<ISet<string>?>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnpublishFails_ReturnsMappedMessage()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new UnpublishUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUnpublish.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentPublishingServiceMock
            .Setup(x => x.UnpublishAsync(key, null, userKey))
            .ReturnsAsync(Attempt<ContentPublishingOperationStatus>.Fail(ContentPublishingOperationStatus.ContentNotFound));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UnpublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithCulture_UnpublishesJustThatCulture()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new UnpublishUmbracoContentArgs(key, "da-DK");
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUnpublish.ActionLetter, key, It.Is<IEnumerable<string>>(c => c.Single() == "da-DK")))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentPublishingServiceMock
            .Setup(x => x.UnpublishAsync(key, It.Is<ISet<string>>(s => s.Single() == "da-DK"), userKey))
            .ReturnsAsync(Attempt<ContentPublishingOperationStatus>.Succeed(ContentPublishingOperationStatus.Success));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UnpublishUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoCulture_UnpublishesAllCultures()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new UnpublishUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUnpublish.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentPublishingServiceMock
            .Setup(x => x.UnpublishAsync(key, null, userKey))
            .ReturnsAsync(Attempt<ContentPublishingOperationStatus>.Succeed(ContentPublishingOperationStatus.Success));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UnpublishUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("offline");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_ContentFound_ReturnsItsName()
    {
        var key = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(Mock.Of<IContent>(c => c.Name == "Home"));

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new UnpublishUmbracoContentArgs(key));

        phrase.ShouldBe("Home");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_ContentNotFound_ReturnsNull()
    {
        var key = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IContent?)null);

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new UnpublishUmbracoContentArgs(key));

        phrase.ShouldBeNull();
    }
}
