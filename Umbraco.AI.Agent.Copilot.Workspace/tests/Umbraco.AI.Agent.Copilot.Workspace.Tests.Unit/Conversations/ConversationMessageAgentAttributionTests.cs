// S8 - Workspace remembers which agent answered (AC1, AC2)
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Assistant messages persisted from a Workspace run carry the ID of the agent that produced them, read
/// from the run's runtime context. That is what Workspace uses as the previous pick for auto selection,
/// and what lets a reopened chat show which agent answered.
/// </summary>
/// <remarks>
/// Assumed seam (T14 may adjust it here): <see cref="ConversationChatHistoryProvider"/> gains an
/// <see cref="IAIRuntimeContextAccessor"/> constructor parameter, after the file store, and
/// <see cref="AIMessage"/> gains <c>Guid? AgentId</c>.
/// </remarks>
public class ConversationMessageAgentAttributionTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();
    private static readonly Guid AgentA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static ConversationChatHistoryProvider CreateProvider(Guid? runningAgentId)
    {
        var accessor = new Mock<IAIRuntimeContextAccessor>();
        if (runningAgentId is not null)
        {
            var context = new AIRuntimeContext([]);
            context.SetValue(AgentConstants.ContextKeys.AgentId, runningAgentId.Value);
            accessor.Setup(x => x.Context).Returns(context);
        }

        return new ConversationChatHistoryProvider(
            Mock.Of<IAIConversationRepository>(),
            Mock.Of<IAIFileStore>(),
            accessor.Object,
            NullLogger<ConversationChatHistoryProvider>.Instance,
            ResentTurnFixtures.Repairer(Mock.Of<IAIConversationRepository>()));
    }

    public class GivenARunByAgentA
    {
        private readonly IReadOnlyList<AIMessage> _stored;

        public GivenARunByAgentA()
        {
            ChatMessage[] request = [new(ChatRole.User, "Name three colours")];
            ChatMessage[] response =
            [
                new(ChatRole.Assistant, "Checking..."),
                new(ChatRole.Tool, "{\"ok\":true}"),
                new(ChatRole.Assistant, "Red, blue, green."),
            ];

            _stored = CreateProvider(AgentA)
                .ToStoredMessagesAsync(ConversationId, request, response)
                .GetAwaiter().GetResult();
        }

        // AC1
        [Fact]
        public void StampsEveryAssistantMessageWithAgentA()
            => _stored.Where(m => m.Role == "assistant").Select(m => m.AgentId).ShouldAllBe(id => id == AgentA);

        // AC2
        [Fact]
        public void LeavesTheUserMessageUnstamped()
            => _stored.Single(m => m.Role == "user").AgentId.ShouldBeNull();

        // AC2
        [Fact]
        public void LeavesTheToolMessageUnstamped()
            => _stored.Single(m => m.Role == "tool").AgentId.ShouldBeNull();
    }

    public class GivenNoRunningAgentInTheRuntimeContext
    {
        private readonly IReadOnlyList<AIMessage> _stored;

        public GivenNoRunningAgentInTheRuntimeContext()
        {
            ChatMessage[] request = [new(ChatRole.User, "Hello")];
            ChatMessage[] response = [new(ChatRole.Assistant, "Hi")];

            _stored = CreateProvider(runningAgentId: null)
                .ToStoredMessagesAsync(ConversationId, request, response)
                .GetAwaiter().GetResult();
        }

        [Fact]
        public void StoresTheAssistantMessageWithNoAgent()
            => _stored.Single(m => m.Role == "assistant").AgentId.ShouldBeNull();
    }

    public class GivenARuntimeContextWithNoAgentIdKey
    {
        private readonly IReadOnlyList<AIMessage> _stored;

        public GivenARuntimeContextWithNoAgentIdKey()
        {
            // A real, active context — just never stamped with an agent id (e.g. a non-agent caller
            // of ToStoredMessagesAsync, or a context populated before PopulateScopeContext runs).
            var accessor = new Mock<IAIRuntimeContextAccessor>();
            accessor.Setup(x => x.Context).Returns(new AIRuntimeContext([]));

            var provider = new ConversationChatHistoryProvider(
                Mock.Of<IAIConversationRepository>(),
                Mock.Of<IAIFileStore>(),
                accessor.Object,
                NullLogger<ConversationChatHistoryProvider>.Instance,
                ResentTurnFixtures.Repairer(Mock.Of<IAIConversationRepository>()));

            ChatMessage[] request = [new(ChatRole.User, "Hello")];
            ChatMessage[] response = [new(ChatRole.Assistant, "Hi")];

            _stored = provider.ToStoredMessagesAsync(ConversationId, request, response).GetAwaiter().GetResult();
        }

        [Fact]
        public void StoresTheAssistantMessageWithNoAgent()
            => _stored.Single(m => m.Role == "assistant").AgentId.ShouldBeNull();
    }
}
