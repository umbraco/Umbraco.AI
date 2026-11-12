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
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Conversations;

/// <summary>
/// A saving-notification handler that cancels a conversation save must surface through the Conversations
/// Management API as a 400 carrying the handler's reason - not a 500 (create) or a misleading 404
/// (update/truncate). Runs the real <see cref="AIConversationService"/> so the exception the controllers
/// catch is the one the service actually throws.
/// </summary>
public class ConversationSaveCancelledApiTests
{
    private const string Reason = "Agent 'nope' is not available in the 'copilot-workspace' surface.";

    private static readonly Guid UserKey = Guid.NewGuid();
    private static readonly Guid ConversationId = Guid.NewGuid();

    [Fact]
    public async Task Create_WhenSaveIsCancelled_Returns400WithTheReason()
    {
        // Arrange
        var harness = new Harness(cancelSaves: true);
        var controller = new CreateConversationController(harness.Service, harness.Mapper);

        // Act
        var result = await controller.Create(new CreateConversationRequestModel { AgentIdOrAlias = "nope" });

        // Assert
        AssertSaveCancelled(result);
    }

    [Fact]
    public async Task Create_WhenSaveIsNotCancelled_Returns201()
    {
        // Arrange
        var harness = new Harness(cancelSaves: false);
        var controller = new CreateConversationController(harness.Service, harness.Mapper);

        // Act
        var result = await controller.Create(new CreateConversationRequestModel { AgentIdOrAlias = "auto" });

        // Assert
        result.ShouldBeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Update_WhenSaveIsCancelled_Returns400WithTheReason()
    {
        // Arrange
        var harness = new Harness(cancelSaves: true);
        var controller = new UpdateConversationController(harness.Service, harness.Mapper);

        // Act
        var result = await controller.Update(ConversationId, new UpdateConversationRequestModel { AgentIdOrAlias = "nope" });

        // Assert
        AssertSaveCancelled(result);
    }

    [Fact]
    public async Task Update_WhenConversationIsMissing_StillReturns404()
    {
        // Arrange
        var harness = new Harness(cancelSaves: true);
        var controller = new UpdateConversationController(harness.Service, harness.Mapper);

        // Act
        var result = await controller.Update(Guid.NewGuid(), new UpdateConversationRequestModel());

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Truncate_WhenSaveIsCancelled_Returns400WithTheReason()
    {
        // Arrange
        var harness = new Harness(cancelSaves: true);
        var controller = new TruncateConversationMessagesController(harness.Service);

        // Act
        var result = await controller.TruncateAfterLastUserMessage(ConversationId);

        // Assert
        AssertSaveCancelled(result);
    }

    private static void AssertSaveCancelled(IActionResult result)
    {
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        var problem = badRequest.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Title.ShouldBe("Conversation save cancelled");
        problem.Detail.ShouldBe(Reason);
    }

    private sealed class Harness
    {
        public Harness(bool cancelSaves)
        {
            var repository = new Mock<IAIConversationRepository>();
            repository
                .Setup(r => r.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIConversation { Id = ConversationId, UserKey = UserKey });
            repository
                .Setup(r => r.CreateAsync(It.IsAny<AIConversation>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIConversation c, CancellationToken _) => c);

            var aggregator = new Mock<IEventAggregator>();
            if (cancelSaves)
            {
                aggregator
                    .Setup(x => x.PublishAsync(It.IsAny<AIConversationSavingNotification>(), It.IsAny<CancellationToken>()))
                    .Callback<AIConversationSavingNotification, CancellationToken>((n, _) =>
                    {
                        n.Messages.Add(new EventMessage("Agent not available", Reason, EventMessageType.Error));
                        n.Cancel = true;
                    })
                    .Returns(Task.CompletedTask);
            }

            var user = new Mock<IUser>();
            user.Setup(x => x.Key).Returns(UserKey);
            var security = new Mock<IBackOfficeSecurity>();
            security.Setup(x => x.CurrentUser).Returns(user.Object);
            var accessor = new Mock<IBackOfficeSecurityAccessor>();
            accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

            Service = new AIConversationService(repository.Object, Mock.Of<IAIFileStore>(), accessor.Object, aggregator.Object);

            Mapper = new UmbracoMapper(
                new MapDefinitionCollection(() => new IMapDefinition[]
                {
                    new ConversationMapDefinition(),
                    new ProjectMapDefinition(),
                }),
                TestCoreScopeProvider.Create(),
                NullLogger<UmbracoMapper>.Instance);
        }

        public IAIConversationService Service { get; }

        public IUmbracoMapper Mapper { get; }
    }
}
