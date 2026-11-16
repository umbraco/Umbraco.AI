// S7 - Workspace auto mode uses my selection rules (AC5 previous-pick source)
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// The service query Workspace uses for its previous pick: the agent on the newest assistant message.
/// Ownership is enforced like every other read, and the "newest assistant row" lookup is the
/// repository's job.
/// </summary>
/// <remarks>
/// Assumed seam (T14 may adjust it here): <c>IAIConversationService.GetLastAssistantAgentIdAsync</c> and
/// <c>IAIConversationRepository.GetLastAssistantAgentIdAsync</c>, both
/// <c>(Guid conversationId, CancellationToken)</c> returning <c>Guid?</c>.
/// </remarks>
public class AIConversationServiceLastAssistantAgentTests
{
    private static readonly Guid UserKey = Guid.NewGuid();
    private static readonly Guid AgentA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    public class GivenAnOwnedConversationWhoseLastReplyWasByAgentA
    {
        private readonly Guid? _result;

        public GivenAnOwnedConversationWhoseLastReplyWasByAgentA()
        {
            var conversationId = Guid.NewGuid();
            var (service, repository) = BuildService();
            repository
                .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
            repository
                .Setup(r => r.GetLastAssistantAgentIdAsync(conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AgentA);

            _result = service.GetLastAssistantAgentIdAsync(conversationId).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsAgentA() => _result.ShouldBe(AgentA);
    }

    public class GivenAnOwnedConversationWithNoAttributedReply
    {
        private readonly Guid? _result;

        public GivenAnOwnedConversationWithNoAttributedReply()
        {
            var conversationId = Guid.NewGuid();
            var (service, repository) = BuildService();
            repository
                .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = UserKey });
            repository
                .Setup(r => r.GetLastAssistantAgentIdAsync(conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid?)null);

            _result = service.GetLastAssistantAgentIdAsync(conversationId).GetAwaiter().GetResult();
        }

        // S7 AC6 (new chat / legacy rows only)
        [Fact]
        public void ReturnsNull() => _result.ShouldBeNull();
    }

    public class GivenAConversationOwnedBySomeoneElse
    {
        private readonly Guid _conversationId = Guid.NewGuid();
        private readonly AIConversationService _service;

        public GivenAConversationOwnedBySomeoneElse()
        {
            var (service, repository) = BuildService();
            repository
                .Setup(r => r.GetByIdAsync(_conversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIConversation { Id = _conversationId, UserKey = Guid.NewGuid() });
            _service = service;
        }

        [Fact]
        public async Task Throws()
            => await Should.ThrowAsync<InvalidOperationException>(() => _service.GetLastAssistantAgentIdAsync(_conversationId));
    }

    private static (AIConversationService Service, Mock<IAIConversationRepository> Repository) BuildService()
    {
        var repository = new Mock<IAIConversationRepository>();

        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(UserKey);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

        var service = new AIConversationService(repository.Object, Mock.Of<IAIFileStore>(), accessor.Object, Mock.Of<IEventAggregator>(), ResentTurnFixtures.Repairer(repository.Object));
        return (service, repository);
    }
}
