using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class DeleteUmbracoMediaToolTests
{
    private readonly Mock<IMediaEditingService> _mediaEditingServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public DeleteUmbracoMediaToolTests()
    {
        _mediaEditingServiceMock = new Mock<IMediaEditingService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new DeleteUmbracoMediaTool(_mediaEditingServiceMock.Object, _authorizerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new DeleteUmbracoMediaArgs(Guid.Empty);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        _authorizerMock.Verify(x => x.AuthorizeMediaAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new DeleteUmbracoMediaArgs(key);
        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _mediaEditingServiceMock.Verify(x => x.MoveToRecycleBinAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_MovesToRecycleBinWithResolvedUserKey()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new DeleteUmbracoMediaArgs(key);
        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _mediaEditingServiceMock
            .Setup(x => x.MoveToRecycleBinAsync(key, userKey))
            .ReturnsAsync(Attempt<IMedia?, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, Mock.Of<IMedia>()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<DeleteUmbracoMediaResult>();
        typed.Success.ShouldBeTrue();
        _mediaEditingServiceMock.Verify(x => x.MoveToRecycleBinAsync(key, userKey), Times.Once);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("recycle bin");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_MediaFound_ReturnsItsName()
    {
        var key = Guid.NewGuid();
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(Mock.Of<IMedia>(m => m.Name == "Logo.png"));

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new DeleteUmbracoMediaArgs(key));

        phrase.ShouldBe("Logo.png");
    }

    [Fact]
    public async Task ResolveConfirmationPhraseAsync_MediaNotFound_ReturnsNull()
    {
        var key = Guid.NewGuid();
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IMedia?)null);

        var phrase = await _tool.ResolveConfirmationPhraseAsync(new DeleteUmbracoMediaArgs(key));

        phrase.ShouldBeNull();
    }
}
