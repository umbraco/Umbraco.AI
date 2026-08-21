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

public class DeleteUmbracoContentToolTests
{
    private readonly Mock<IContentEditingService> _contentEditingServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public DeleteUmbracoContentToolTests()
    {
        _contentEditingServiceMock = new Mock<IContentEditingService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new DeleteUmbracoContentTool(_contentEditingServiceMock.Object, _authorizerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new DeleteUmbracoContentArgs(Guid.Empty);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new DeleteUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionDelete.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _contentEditingServiceMock.Verify(
            x => x.MoveToRecycleBinAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DeleteFails_ReturnsMappedMessage()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new DeleteUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionDelete.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.MoveToRecycleBinAsync(key, userKey))
            .ReturnsAsync(Attempt<IContent?, ContentEditingOperationStatus>.Fail(
                ContentEditingOperationStatus.NotFound, (IContent?)null));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_MovesToRecycleBinWithResolvedUserKey()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new DeleteUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionDelete.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.MoveToRecycleBinAsync(key, userKey))
            .ReturnsAsync(Attempt<IContent?, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, Mock.Of<IContent>()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
        _contentEditingServiceMock.Verify(x => x.MoveToRecycleBinAsync(key, userKey), Times.Once);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("recycle bin");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_ContentFound_ReturnsItsName()
    {
        var key = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(Mock.Of<IContent>(c => c.Name == "Home"));

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new DeleteUmbracoContentArgs(key));

        phrase.ShouldBe("Home");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_ContentNotFound_ReturnsNull()
    {
        var key = Guid.NewGuid();
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IContent?)null);

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new DeleteUmbracoContentArgs(key));

        phrase.ShouldBeNull();
    }
}
