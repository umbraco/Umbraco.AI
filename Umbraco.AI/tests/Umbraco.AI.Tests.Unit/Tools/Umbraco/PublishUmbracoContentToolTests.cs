using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class PublishUmbracoContentToolTests
{
    private readonly Mock<IContentPublishingService> _contentPublishingServiceMock;
    private readonly Mock<IContentService> _contentServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public PublishUmbracoContentToolTests()
    {
        _contentPublishingServiceMock = new Mock<IContentPublishingService>();
        _contentServiceMock = new Mock<IContentService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new PublishUmbracoContentTool(_contentPublishingServiceMock.Object, _contentServiceMock.Object, _authorizerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new PublishUmbracoContentArgs(Guid.Empty);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<PublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new PublishUmbracoContentArgs(key, "da-DK");
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionPublish.ActionLetter, key, It.Is<IEnumerable<string>>(c => c.Single() == "da-DK")))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<PublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _contentPublishingServiceMock.Verify(
            x => x.PublishAsync(It.IsAny<Guid>(), It.IsAny<ICollection<CulturePublishScheduleModel>>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PublishFails_ReturnsMappedMessage()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new PublishUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionPublish.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentPublishingServiceMock
            .Setup(x => x.PublishAsync(key, It.IsAny<ICollection<CulturePublishScheduleModel>>(), userKey))
            .ReturnsAsync(Attempt<ContentPublishingResult, ContentPublishingOperationStatus>.Fail(
                ContentPublishingOperationStatus.MandatoryCultureMissing, new ContentPublishingResult()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<PublishUmbracoContentResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("mandatory culture");
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_PublishesWithResolvedUserKeyAndInvariantCulture()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new PublishUmbracoContentArgs(key);
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionPublish.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        ICollection<CulturePublishScheduleModel>? captured = null;
        _contentPublishingServiceMock
            .Setup(x => x.PublishAsync(key, It.IsAny<ICollection<CulturePublishScheduleModel>>(), userKey))
            .Callback<Guid, ICollection<CulturePublishScheduleModel>, Guid>((_, schedules, _) => captured = schedules)
            .ReturnsAsync(Attempt<ContentPublishingResult, ContentPublishingOperationStatus>.Succeed(
                ContentPublishingOperationStatus.Success, new ContentPublishingResult()));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<PublishUmbracoContentResult>();
        typed.Success.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Single().Culture.ShouldBeNull();
        captured.Single().Schedule.ShouldBeNull();
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
        description.ShouldContain("live");
    }

    [Fact]
    public void ConfirmationPhrase_ContentFound_ReturnsItsName()
    {
        var key = Guid.NewGuid();
        _contentServiceMock.Setup(x => x.GetById(key)).Returns(Mock.Of<IContent>(c => c.Name == "Home"));

        var phrase = _tool.ConfirmationPhrase(new PublishUmbracoContentArgs(key));

        phrase.ShouldBe("Home");
    }

    [Fact]
    public void ConfirmationPhrase_ContentNotFound_ReturnsNull()
    {
        var key = Guid.NewGuid();
        _contentServiceMock.Setup(x => x.GetById(key)).Returns((IContent?)null);

        var phrase = _tool.ConfirmationPhrase(new PublishUmbracoContentArgs(key));

        phrase.ShouldBeNull();
    }
}
