using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Conversations;

/// <summary>
/// A Snapshot share that can't be anchored to a message (<see cref="AIConversationShareCutoffUnresolvableException"/>)
/// must surface through the Management API as a 400 - never miscategorized as the 404
/// <see cref="ConversationControllerBase.ConversationNotFound"/> covers for an unowned/missing
/// conversation, even though the exception derives from <see cref="InvalidOperationException"/>. Also
/// covers the plain ownership-check 404 on all three share controllers.
/// </summary>
public class ConversationShareControllersTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid RecipientUserKey = Guid.NewGuid();

    [Fact]
    public async Task Share_WhenCutoffIsUnresolvable_Returns400()
    {
        // Arrange
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.ShareConversationAsync(
                ConversationId, It.IsAny<IReadOnlyList<Guid>>(), AIConversationShareScopeMode.Snapshot, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AIConversationShareCutoffUnresolvableException(ConversationId));
        var controller = new ShareConversationController(shareService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.Share(
            ConversationId,
            new ShareConversationRequestModel { RecipientUserKeys = [RecipientUserKey], ScopeMode = "Snapshot" });

        // Assert — must be a 400, not the 404 the ownership check (below) also returns for the same
        // exception base type.
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Title.ShouldBe("Conversation share cutoff unresolvable");
    }

    [Fact]
    public async Task Share_WhenConversationIsNotOwned_Returns404()
    {
        // Arrange — the plain InvalidOperationException the ownership check throws, not the
        // cutoff-specific subclass above.
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.ShareConversationAsync(
                ConversationId, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<AIConversationShareScopeMode>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not owned"));
        var controller = new ShareConversationController(shareService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.Share(
            ConversationId,
            new ShareConversationRequestModel { RecipientUserKeys = [RecipientUserKey], ScopeMode = "Live" });

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("7")] // Numeric strings parse via Enum.TryParse even with no matching named member.
    public async Task Share_WhenScopeModeIsUnrecognized_Returns400WithoutCallingTheService(string scopeMode)
    {
        // Arrange
        var shareService = new Mock<IAIConversationShareService>();
        var controller = new ShareConversationController(shareService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.Share(
            ConversationId,
            new ShareConversationRequestModel { RecipientUserKeys = [RecipientUserKey], ScopeMode = scopeMode });

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        shareService.Verify(
            s => s.ShareConversationAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<AIConversationShareScopeMode>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Share_WhenSuccessful_Returns200WithResolvedUserNames()
    {
        // Arrange
        var share = new AIConversationShare
        {
            ConversationId = ConversationId,
            SharedWithUserKey = RecipientUserKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.ShareConversationAsync(
                ConversationId, It.IsAny<IReadOnlyList<Guid>>(), AIConversationShareScopeMode.Live, It.IsAny<CancellationToken>()))
            .ReturnsAsync([share]);

        var recipient = new Mock<IUser>();
        recipient.Setup(u => u.Name).Returns("Dagny Hansen");
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(RecipientUserKey)).ReturnsAsync(recipient.Object);

        var controller = new ShareConversationController(shareService.Object, userService.Object, BuildMapper());

        // Act
        var result = await controller.Share(
            ConversationId,
            new ShareConversationRequestModel { RecipientUserKeys = [RecipientUserKey], ScopeMode = "Live" });

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var models = ok.Value.ShouldBeAssignableTo<IReadOnlyList<ConversationShareResponseModel>>()!;
        models.ShouldHaveSingleItem();
        models[0].UserKey.ShouldBe(RecipientUserKey);
        models[0].UserName.ShouldBe("Dagny Hansen");
        models[0].ScopeMode.ShouldBe("Live");
    }

    [Fact]
    public async Task GetShares_WhenConversationIsNotOwned_Returns404()
    {
        // Arrange
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.GetConversationSharesAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not owned"));
        var controller = new GetConversationSharesController(shareService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.GetShares(ConversationId);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetShares_WhenSuccessful_Returns200WithResolvedUserNames()
    {
        // Arrange
        var share = new AIConversationShare
        {
            ConversationId = ConversationId,
            SharedWithUserKey = RecipientUserKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.GetConversationSharesAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([share]);

        var recipient = new Mock<IUser>();
        recipient.Setup(u => u.Name).Returns("Dagny Hansen");
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(RecipientUserKey)).ReturnsAsync(recipient.Object);

        var controller = new GetConversationSharesController(shareService.Object, userService.Object, BuildMapper());

        // Act
        var result = await controller.GetShares(ConversationId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var models = ok.Value.ShouldBeAssignableTo<IReadOnlyList<ConversationShareResponseModel>>()!;
        models.ShouldHaveSingleItem();
        models[0].UserKey.ShouldBe(RecipientUserKey);
        models[0].UserName.ShouldBe("Dagny Hansen");
        models[0].ScopeMode.ShouldBe("Live");
    }

    [Fact]
    public async Task ConversationShareResponseModelFactory_WhenRecipientIsUnknown_FallsBackToUnknownUserDisplayName()
    {
        // Arrange — IUserService.GetAsync returns null (recipient deleted or otherwise unresolvable).
        var share = new AIConversationShare
        {
            ConversationId = ConversationId,
            SharedWithUserKey = RecipientUserKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(RecipientUserKey)).ReturnsAsync((IUser?)null);

        // Act
        var models = await ConversationShareResponseModelFactory.MapAsync(BuildMapper(), userService.Object, [share]);

        // Assert
        models.ShouldHaveSingleItem();
        models[0].UserName.ShouldBe("Unknown user");
    }

    [Fact]
    public async Task Revoke_WhenNoActiveShareExists_Returns404()
    {
        // Arrange — "not owned" and "no active share for that recipient" are deliberately
        // indistinguishable, both surfacing as the plain InvalidOperationException/404.
        var shareService = new Mock<IAIConversationShareService>();
        shareService
            .Setup(s => s.RevokeConversationShareAsync(ConversationId, RecipientUserKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("no active share"));
        var controller = new RevokeConversationShareController(shareService.Object);

        // Act
        var result = await controller.Revoke(ConversationId, RecipientUserKey);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Revoke_WhenSuccessful_Returns204()
    {
        // Arrange
        var shareService = new Mock<IAIConversationShareService>();
        var controller = new RevokeConversationShareController(shareService.Object);

        // Act
        var result = await controller.Revoke(ConversationId, RecipientUserKey);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    private static IUmbracoMapper BuildMapper()
        => new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new ConversationMapDefinition(),
                new ConversationShareMapDefinition(),
                new ProjectMapDefinition(),
            }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);
}
