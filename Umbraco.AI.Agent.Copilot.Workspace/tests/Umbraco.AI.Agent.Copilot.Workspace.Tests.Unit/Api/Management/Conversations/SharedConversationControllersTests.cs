using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Controllers;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Conversations;

/// <summary>
/// SC-09 — the recipient-side Management API controllers that sit in front of
/// <see cref="IAIConversationShareReadService"/>: <see cref="SharedWithMeConversationsController"/>
/// (<c>GET /conversations/shared-with-me</c>) and <see cref="SharedViewConversationController"/>
/// (<c>GET /conversations/{id}/shared-view</c>).
/// </summary>
public class SharedConversationControllersTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid OwnerUserKey = Guid.NewGuid();

    [Fact]
    public async Task GetSharedWithMe_WhenSuccessful_Returns200WithResolvedOwnerNames()
    {
        // Arrange
        var summary = new AISharedConversationSummary
        {
            ConversationId = ConversationId,
            Title = "Debugging the checkout redirect",
            OwnerUserKey = OwnerUserKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            DateShared = DateTime.UtcNow,
        };
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationsPagedAsync(0, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { summary }, 1));

        var owner = new Mock<IUser>();
        owner.Setup(u => u.Name).Returns("Dagny Hansen");
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(OwnerUserKey)).ReturnsAsync(owner.Object);

        var controller = new SharedWithMeConversationsController(shareReadService.Object, userService.Object, BuildMapper());

        // Act
        var result = await controller.GetSharedWithMe();

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var page = ok.Value.ShouldBeOfType<PagedViewModel<SharedConversationSummaryResponseModel>>();
        page.Total.ShouldBe(1);
        page.Items.ShouldHaveSingleItem();
        var item = page.Items.Single();
        item.ConversationId.ShouldBe(ConversationId);
        item.Title.ShouldBe("Debugging the checkout redirect");
        item.OwnerDisplayName.ShouldBe("Dagny Hansen");
        item.ScopeMode.ShouldBe("Live");
    }

    [Fact]
    public async Task GetSharedWithMe_WhenOwnerIsUnknown_FallsBackToUnknownUserDisplayName()
    {
        // Arrange
        var summary = new AISharedConversationSummary
        {
            ConversationId = ConversationId,
            OwnerUserKey = OwnerUserKey,
            ScopeMode = AIConversationShareScopeMode.Snapshot,
            DateShared = DateTime.UtcNow,
        };
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationsPagedAsync(0, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { summary }, 1));

        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(OwnerUserKey)).ReturnsAsync((IUser?)null);

        var controller = new SharedWithMeConversationsController(shareReadService.Object, userService.Object, BuildMapper());

        // Act
        var result = await controller.GetSharedWithMe();

        // Assert
        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var page = ok.Value.ShouldBeOfType<PagedViewModel<SharedConversationSummaryResponseModel>>();
        page.Items.Single().OwnerDisplayName.ShouldBe("Unknown user");
    }

    [Fact]
    public async Task GetSharedWithMe_PassesSearchStraightThroughWithoutWidening()
    {
        // Arrange — the title-only scoping is IAIConversationShareReadService's job (see
        // FindSharedConversationsTests / AIConversationShareReadService), not the controller's. This
        // guards the controller side of that contract: the controller must not touch, widen, or
        // otherwise alter the caller-supplied search term (e.g. by also matching message content)
        // before handing it to the service — doing so would reopen the cutoff-leak the title-only
        // narrowing exists to close.
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationsPagedAsync(5, 20, "checkout", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AISharedConversationSummary>(), 0));

        var controller = new SharedWithMeConversationsController(shareReadService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        await controller.GetSharedWithMe(search: "checkout", skip: 5, take: 20);

        // Assert
        shareReadService.Verify(
            s => s.GetSharedConversationsPagedAsync(5, 20, "checkout", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSharedView_WhenNoActiveShareExists_Returns404()
    {
        // Arrange
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((AIConversation Conversation, IReadOnlyList<AIMessage> Messages, AIConversationShareScopeMode ScopeMode)?)null);
        var controller = new SharedViewConversationController(shareReadService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.GetSharedView(ConversationId);

        // Assert — same generic not-found wording every other endpoint uses, so wording drift here
        // would actually fail this test instead of passing silently.
        var notFound = result.ShouldBeOfType<NotFoundObjectResult>();
        var problem = notFound.Value.ShouldBeOfType<ProblemDetails>();
        problem.Title.ShouldBe("Conversation not found");
    }

    [Fact]
    public async Task GetSharedView_WhenSuccessful_Returns200WithOwnerNameAndMessages()
    {
        // Arrange
        var resource = new AIAttachedResource { Id = Guid.NewGuid(), ResourceTypeId = "content" };
        var contextId = Guid.NewGuid();
        var conversation = new AIConversation
        {
            Id = ConversationId,
            UserKey = OwnerUserKey,
            Title = "Debugging the checkout redirect",
            ContextIds = [contextId],
            Resources = [resource],
        };
        var messages = new[]
        {
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1, Role = "user", ContentJson = "{}" },
            new AIMessage { Id = Guid.NewGuid(), Sequence = 2, Role = "assistant", ContentJson = "{}" },
        };
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((conversation, (IReadOnlyList<AIMessage>)messages, AIConversationShareScopeMode.Live));

        var owner = new Mock<IUser>();
        owner.Setup(u => u.Name).Returns("Dagny Hansen");
        var userService = new Mock<IUserService>();
        userService.Setup(u => u.GetAsync(OwnerUserKey)).ReturnsAsync(owner.Object);

        var controller = new SharedViewConversationController(shareReadService.Object, userService.Object, BuildMapper());

        // Act
        var result = await controller.GetSharedView(ConversationId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var model = ok.Value.ShouldBeOfType<SharedConversationResponseModel>();
        model.Id.ShouldBe(ConversationId);
        model.Title.ShouldBe("Debugging the checkout redirect");
        model.OwnerDisplayName.ShouldBe("Dagny Hansen");
        model.ContextIds.ShouldBe([contextId]);
        model.Resources.Select(r => r.Id).ShouldBe([resource.Id]);
        model.Messages.Select(m => m.Sequence).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task GetSharedView_SnapshotScope_FreezesDatesAtCutoffMessage()
    {
        // ARCHITECTURE.md decision 9 / STORIES.md US4 AC7 — simulate the owner having kept chatting
        // after sharing: the conversation's own live DateModified/LastMessageAt are later than the
        // cutoff message's own DateCreated, but the response must still only disclose the cutoff date.
        var cutoffDate = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var liveDate = cutoffDate.AddDays(3);
        var conversation = new AIConversation
        {
            Id = ConversationId,
            UserKey = OwnerUserKey,
            Title = "Debugging the checkout redirect",
            DateModified = liveDate,
            LastMessageAt = liveDate,
        };
        var cutoffMessage = new AIMessage { Id = Guid.NewGuid(), Sequence = 2, Role = "assistant", ContentJson = "{}", DateCreated = cutoffDate };
        var messages = new[]
        {
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1, Role = "user", ContentJson = "{}", DateCreated = cutoffDate.AddMinutes(-5) },
            cutoffMessage,
        };
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((conversation, (IReadOnlyList<AIMessage>)messages, AIConversationShareScopeMode.Snapshot));

        var controller = new SharedViewConversationController(shareReadService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.GetSharedView(ConversationId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var model = ok.Value.ShouldBeOfType<SharedConversationResponseModel>();
        model.DateModified.ShouldBe(cutoffDate);
        model.LastMessageAt.ShouldBe(cutoffDate);
    }

    [Fact]
    public async Task GetSharedView_LiveScope_DoesNotOverrideDates()
    {
        // ARCHITECTURE.md decision 9 — a Live share has no ceiling, so its dates stay exactly the
        // conversation's own live values, even though messages happen to end before DateModified.
        var liveDate = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);
        var conversation = new AIConversation
        {
            Id = ConversationId,
            UserKey = OwnerUserKey,
            Title = "Debugging the checkout redirect",
            DateModified = liveDate,
            LastMessageAt = liveDate,
        };
        var messages = new[]
        {
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1, Role = "user", ContentJson = "{}", DateCreated = liveDate.AddMinutes(-30) },
        };
        var shareReadService = new Mock<IAIConversationShareReadService>();
        shareReadService
            .Setup(s => s.GetSharedConversationAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((conversation, (IReadOnlyList<AIMessage>)messages, AIConversationShareScopeMode.Live));

        var controller = new SharedViewConversationController(shareReadService.Object, Mock.Of<IUserService>(), BuildMapper());

        // Act
        var result = await controller.GetSharedView(ConversationId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        var model = ok.Value.ShouldBeOfType<SharedConversationResponseModel>();
        model.DateModified.ShouldBe(liveDate);
        model.LastMessageAt.ShouldBe(liveDate);
    }

    private static IUmbracoMapper BuildMapper()
        => new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new ConversationMapDefinition(),
                new ConversationShareMapDefinition(),
                new SharedConversationMapDefinition(),
                new ProjectMapDefinition(),
            }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);
}
