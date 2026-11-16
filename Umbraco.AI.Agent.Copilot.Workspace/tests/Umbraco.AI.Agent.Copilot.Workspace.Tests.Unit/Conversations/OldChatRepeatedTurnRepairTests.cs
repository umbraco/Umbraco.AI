// S2 - An old chat with a repeated turn repairs itself (AC1-AC6)
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using Xunit;

using static Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: an old chat that already holds a repeated turn repairs itself when read.
/// AC1, AC4 and AC5 also run against <see cref="AIResentTurnMerger"/> directly (T2); the rest run through the real
/// <see cref="AIConversationService"/> (read for display) and <see cref="ConversationChatHistoryProvider"/> (load for a run) (T4).
/// </summary>
public class OldChatRepeatedTurnRepairTests
{
    // The stored rows from AC1: U, call C, the stand-in for C, then U again, call C again and the real result for C.
    private static List<AIMessage> RepeatedRows() =>
        Rows(
            User("u1"),
            Call("a1", "C"),
            Result("t1", ("C", StandIn)),
            User("u1"),
            Call("a2", "C"),
            Result("t2", ("C", "real")));

    private static readonly string[] RepairedHistory = ["user:u1", "call:C", "result:C=real"];

    private static readonly string[] UnrepairedHistory =
        ["user:u1", "call:C", "result:C=" + StandIn, "user:u1", "call:C", "result:C=real"];

    // Scenario: a chat holding a repeated turn (happy path)
    // Given (AC1-AC3): the store holds U, call C, the stand-in for C, then U again, call C again and the real result for C.

    // AC1 (merger level) - Then the returned messages hold U once, call C once and the real result for C
    [Fact]
    public void ReadForDisplay_ReturnsTheRepeatedTurnOnce()
    {
        var rows = RepeatedRows();

        var remaining = Apply(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        remaining.Select(Describe).ShouldBe(RepairedHistory);
    }

    // AC1 (service level) - the messages the endpoint returns hold U once, call C once and the real result for C
    [Fact]
    public async Task ReadForDisplay_ThroughTheService_ReturnsTheRepeatedTurnOnce()
    {
        var chat = new FakeChat(RepeatedRows());

        var messages = await chat.ReadForDisplayAsync();

        messages.Select(DescribeRow).ShouldBe(RepairedHistory);
    }

    // AC2 - Then the model gets U once, call C once and the real result for C
    [Fact]
    public async Task LoadForRun_SendsTheRepeatedTurnToTheModelOnce()
    {
        var chat = new FakeChat(RepeatedRows());

        var history = await chat.LoadForRunAsync();

        history.Select(Describe).ShouldBe(RepairedHistory);
    }

    // AC3 - Then the store no longer holds the second copy of U and C. Covered at both call sites: display, then before a run.
    [Fact]
    public async Task ReadForDisplay_RemovesTheRepeatedRowsFromTheStore()
    {
        var chat = new FakeChat(RepeatedRows());

        await chat.ReadForDisplayAsync();

        chat.StoredDescriptions.ShouldBe(RepairedHistory);
    }

    [Fact]
    public async Task LoadForRun_RemovesTheRepeatedRowsFromTheStore()
    {
        var chat = new FakeChat(RepeatedRows());

        await chat.LoadForRunAsync();

        chat.StoredDescriptions.ShouldBe(RepairedHistory);
    }

    // Scenario: sad path and edges

    // AC4 (merger level) - Given a chat already repaired; when its messages are read again, then no database write happens
    [Fact]
    public void SecondRead_OfARepairedChat_WritesNothing()
    {
        var rows = RepeatedRows();
        var repairedRows = ApplyRows(rows, AIResentTurnMerger.RepairRepeatedTurns(rows));

        AIResentTurnMerger.RepairRepeatedTurns(repairedRows).IsEmpty.ShouldBeTrue();
    }

    // AC4 (service level) - a second read of a repaired chat writes nothing more
    [Fact]
    public async Task SecondRead_ThroughTheService_WritesNothingMore()
    {
        var chat = new FakeChat(RepeatedRows());
        await chat.ReadForDisplayAsync();

        await chat.ReadForDisplayAsync();

        chat.Rewrites.ShouldBe(1);
    }

    // AC5 (merger level) - Given a chat with no repeated turn; when its messages are read, then no database write happens
    [Fact]
    public void Read_OfAChatWithNoRepeat_WritesNothing()
    {
        var rows = Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real")), User("u2"), Call("a2", "D"), Result("t2", ("D", "real")));

        AIResentTurnMerger.RepairRepeatedTurns(rows).IsEmpty.ShouldBeTrue();
    }

    // AC5 (service and provider level) - a chat with no repeat is not written to, at either call site
    [Fact]
    public async Task ReadForDisplay_OfAChatWithNoRepeat_WritesNothing()
    {
        var chat = new FakeChat(Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real"))));

        await chat.ReadForDisplayAsync();

        chat.Rewrites.ShouldBe(0);
    }

    [Fact]
    public async Task LoadForRun_OfAChatWithNoRepeat_WritesNothing()
    {
        var chat = new FakeChat(Rows(User("u1"), Call("a1", "C"), Result("t1", ("C", "real"))));

        await chat.LoadForRunAsync();

        chat.Rewrites.ShouldBe(0);
    }

    // AC6 - Given the store fails partway through a repair; when the messages are read,
    // then the stored rows are unchanged and the read returns the unrepaired messages
    [Fact]
    public async Task ReadForDisplay_WhenTheRepairFails_ReturnsTheUnrepairedMessages()
    {
        var chat = new FakeChat(RepeatedRows()) { RewriteFailure = new InvalidOperationException("store failed") };

        var messages = await chat.ReadForDisplayAsync();

        messages.Select(DescribeRow).ShouldBe(UnrepairedHistory);
    }

    [Fact]
    public async Task LoadForRun_WhenTheRepairFails_ReturnsTheUnrepairedHistory()
    {
        var chat = new FakeChat(RepeatedRows()) { RewriteFailure = new InvalidOperationException("store failed") };

        var history = await chat.LoadForRunAsync();

        history.Select(Describe).ShouldBe(UnrepairedHistory);
    }

    [Fact]
    public async Task ReadForDisplay_WhenTheRepairFails_LeavesTheStoredRowsAsTheyWere()
    {
        var chat = new FakeChat(RepeatedRows()) { RewriteFailure = new InvalidOperationException("store failed") };

        await chat.ReadForDisplayAsync();

        chat.StoredDescriptions.ShouldBe(UnrepairedHistory);
    }

    // A cancelled token is the caller giving up, not a failed repair: it propagates.
    [Fact]
    public async Task LoadForRun_WhenTheCallerCancels_Propagates()
    {
        var chat = new FakeChat(RepeatedRows()) { RewriteFailure = new OperationCanceledException() };

        await Should.ThrowAsync<OperationCanceledException>(() => chat.LoadForRunAsync());
    }

    // Ownership: the repair runs only after the ownership check passes.
    [Fact]
    public async Task ReadForDisplay_OfAnotherUsersChat_WritesNothing()
    {
        var chat = new FakeChat(RepeatedRows()) { Owner = Guid.NewGuid() };

        await Should.ThrowAsync<InvalidOperationException>(() => chat.ReadForDisplayAsync());

        chat.Rewrites.ShouldBe(0);
    }

    private static string DescribeRow(AIMessage row) => Describe(AIStoredMessageMapper.TryDeserialize(row.ContentJson)!);

    /// <summary>
    /// The real <see cref="AIConversationService"/> and <see cref="ConversationChatHistoryProvider"/> over one fake
    /// repository that holds the rows and applies the rewrites it is given (or throws <see cref="RewriteFailure"/>).
    /// </summary>
    private sealed class FakeChat
    {
        private static readonly Guid ConversationId = Guid.NewGuid();
        private static readonly Guid UserKey = Guid.NewGuid();

        private readonly Mock<IAIConversationRepository> _repository = new();

        public FakeChat(List<AIMessage> rows)
        {
            Store = rows;
            _repository
                .Setup(x => x.GetMessagesAsync(ConversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Store.ToList());
            _repository
                .Setup(x => x.GetByIdAsync(ConversationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new AIConversation { Id = ConversationId, UserKey = Owner ?? UserKey });
            _repository
                .Setup(x => x.GetMessagesPagedAsync(ConversationId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, int skip, int take, CancellationToken _) =>
                    ((IReadOnlyList<AIMessage>)Store.Skip(skip).Take(take).ToList(), Store.Count));
            _repository
                .Setup(x => x.RewriteMessagesAsync(ConversationId, It.IsAny<IReadOnlyList<AIMessage>>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
                .Callback((Guid _, IReadOnlyList<AIMessage> updates, IReadOnlyList<Guid> deleteIds, CancellationToken _) =>
                {
                    Rewrites++;
                    if (RewriteFailure is not null)
                    {
                        throw RewriteFailure;
                    }

                    Store.RemoveAll(r => deleteIds.Contains(r.Id));
                    foreach (var update in updates)
                    {
                        Store[Store.FindIndex(r => r.Id == update.Id)] = update;
                    }
                })
                .Returns(Task.CompletedTask);
        }

        public List<AIMessage> Store { get; }

        /// <summary>The owner of the stored conversation; the acting user when null.</summary>
        public Guid? Owner { get; init; }

        public Exception? RewriteFailure { get; init; }

        /// <summary>How many times a rewrite was attempted.</summary>
        public int Rewrites { get; private set; }

        public List<string> StoredDescriptions => Store.Select(DescribeRow).ToList();

        public async Task<IReadOnlyList<AIMessage>> ReadForDisplayAsync()
        {
            var user = new Mock<IUser>();
            user.Setup(x => x.Key).Returns(UserKey);
            var security = new Mock<IBackOfficeSecurity>();
            security.Setup(x => x.CurrentUser).Returns(user.Object);
            var accessor = new Mock<IBackOfficeSecurityAccessor>();
            accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

            var service = new AIConversationService(
                _repository.Object, Mock.Of<IAIFileStore>(), accessor.Object, Mock.Of<IEventAggregator>(), Repairer(_repository.Object));

            return (await service.GetMessagesPagedAsync(ConversationId, 0, 100)).Items;
        }

        public async Task<List<ChatMessage>> LoadForRunAsync()
        {
            var provider = new ConversationChatHistoryProvider(
                _repository.Object,
                Mock.Of<IAIFileStore>(),
                Mock.Of<IAIRuntimeContextAccessor>(),
                NullLogger<ConversationChatHistoryProvider>.Instance,
                Repairer(_repository.Object));

            var session = new TestSession();
            provider.BindConversation(session, ConversationId);
#pragma warning disable MAAI001 // InvokingContext is marked evaluation-only; it is the public way to run ProvideChatHistoryAsync.
            var history = await provider.InvokingAsync(new ChatHistoryProvider.InvokingContext(Mock.Of<AIAgent>(), session, []));
#pragma warning restore MAAI001
            return history.ToList();
        }

        private static string DescribeRow(AIMessage row) => ResentTurnFixtures.Describe(AIStoredMessageMapper.TryDeserialize(row.ContentJson)!);

        private sealed class TestSession : AgentSession;
    }
}
