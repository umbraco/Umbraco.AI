// S1 - A frontend-tool turn is stored once (AC1-AC8)
using System.Text.Json;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.Core.RuntimeContext;

using Xunit;

using static Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations.ResentTurnFixtures;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Feature: a turn where the agent used a browser (frontend) tool is stored once, with the real tool result.
/// AC1-AC7 run against <see cref="AIResentTurnMerger"/> directly (T2); the provider-level counterparts follow below (T3).
/// </summary>
public class FrontendToolTurnStoredOnceTests
{
    // The stored turn from AC1: U, call C, the stand-in for C. The browser resends U, its copy of C and the real result.
    private static List<AIMessage> StoredTurn() => Rows(User("u1"), Call("srv-a", "C"), Result("srv-t", ("C", StandIn)));

    private static List<ChatMessage> ResentTurn() => [User("u1"), Call("br-a", "C"), Result("br-t", ("C", "real"))];

    private static AIResentTurnMerger.ResentTurnResult Merge(List<AIMessage> stored, List<ChatMessage> incoming)
        => MergeResentTurn(stored, incoming);

    // Scenario: the browser resends a turn the store already holds (happy path)
    // Given (AC1-AC6): the store holds user message U, tool call C and the stand-in result for C;
    // the browser resends U (same id), its copy of call C and the real result for C.
    // AC5 and AC6 start from their own stores, noted on each.

    // AC1 - Then U is not passed to the model a second time
    [Fact]
    public void ResentTurn_WhenUserMessageResent_DropsItFromWhatTheModelGets()
    {
        var result = Merge(StoredTurn(), ResentTurn());

        result.Candidates.Any(m => m.Role == ChatRole.User).ShouldBeFalse();
    }

    // AC2 - Then the browser's assistant message with call C is not passed to the model
    [Fact]
    public void ResentTurn_WhenToolCallResent_DropsTheBrowsersAssistantMessage()
    {
        var result = Merge(StoredTurn(), ResentTurn());

        result.Candidates.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ShouldBeEmpty();
    }

    // AC3 - Then the stored result for call C holds the real result, not "Success: Function completed."
    [Fact]
    public void ResentTurn_WhenRealResultArrives_ReplacesTheStandInInTheStore()
    {
        var stored = StoredTurn();
        var result = Merge(stored, ResentTurn());

        Updated(stored, result).ShouldBe([(2, "result:C=real")]);
    }

    // AC4 - Then the conversation stores U once, call C once and one result for C
    [Fact]
    public void ResentTurn_WhenRunFinishes_StoresUserMessageCallAndResultOnce()
    {
        var stored = StoredTurn();
        var result = Merge(stored, ResentTurn());

        // What the store holds after the merge: the stored messages with the updates written, then whatever is passed on.
        var afterRun = stored
            .Select(r => result.RowsToUpdate.FirstOrDefault(u => u.Id == r.Id) ?? r)
            .Select(r => JsonSerializer.Deserialize<ChatMessage>(r.ContentJson, AIJsonUtilities.DefaultOptions)!)
            .ToList();

        afterRun.AddRange(result.Candidates);

        afterRun.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC5 - Given the store holds U, server call S with its real result, frontend call F and the stand-in for F;
    // the browser resends U, calls S and F, and the real results for S and F.
    // Then the stored result for S is unchanged and the stored result for F holds the real result
    [Fact]
    public void ResentTurn_WithServerToolBeforeFrontendTool_LeavesServerResultUnchangedAndUpdatesFrontendResult()
    {
        var stored = Rows(
            User("u1"),
            Call("srv-a1", "S"),
            Result("srv-t1", ("S", "server-result")),
            Call("srv-a2", "F"),
            Result("srv-t2", ("F", StandIn)));
        List<ChatMessage> incoming =
        [
            User("u1"),
            Call("br-a", "S", "F"),
            Result("br-t", ("S", "server-result-again"), ("F", "ui-result")),
        ];

        var result = Merge(stored, incoming);

        // Only the stand-in for F is rewritten (stored index 4); the stored result for S is not touched.
        Updated(stored, result).ShouldBe([(4, "result:F=ui-result")]);
    }

    // AC6 - When the resent turn is followed by a new user message N, then N is passed to the model and stored
    [Fact]
    public void ResentTurn_FollowedByNewUserMessage_PassesAndStoresTheNewMessage()
    {
        var result = Merge(StoredTurn(), [.. ResentTurn(), User("n1", "Next question")]);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["n1"]);
    }

    // Scenario: sad path and edges

    // AC7 - Given the store holds a finished turn starting with user message U1 "Hello"; when a new run sends U2 "Hello" with a different id,
    // then U2 is passed to the model and stored as a new turn
    [Fact]
    public void SameTextWithDifferentId_IsTreatedAsANewTurn()
    {
        List<ChatMessage> incoming = [User("u2", "Hello")];

        var result = Merge(Rows(User("u1", "Hello"), Call("a", "C"), Result("t", ("C", "done"))), incoming);

        result.Candidates.Select(m => m.MessageId).ShouldBe(["u2"]);
    }

    // AC8 (an approval resume takes the existing approval path, no merge) is a spec on the AG-UI stream, where the resume
    // branch lives: AGUIStreamingServiceTests.StreamAgentAsync_WithResume_DoesNotCallTheDropAlreadyPersistedCallback
    // in Umbraco.AI.Agent.Tests.Unit. The merge only runs from the drop callback that branch never calls.
}

/// <summary>
/// The same feature through <see cref="ConversationChatHistoryProvider"/> (T3): the real provider over a repository that
/// holds the stored turn and applies the rewrites it is given. "What the model gets" is the stored history the provider
/// loads for the run, followed by the messages the merge passes on.
/// </summary>
public class FrontendToolTurnStoredOnceThroughProviderTests
{
    private static readonly Guid ConversationId = Guid.NewGuid();

    private static List<AIMessage> StoredTurn() => Rows(User("u1"), Call("srv-a", "C"), Result("srv-t", ("C", StandIn)));

    private static List<ChatMessage> ResentTurn() => [User("u1"), Call("br-a", "C"), Result("br-t", ("C", "real"))];

    private sealed class TestSession : AgentSession;

    private sealed class Run
    {
        public required Mock<IAIConversationRepository> Repository { get; init; }
        public required List<AIMessage> Store { get; init; }
        public required IReadOnlyList<ChatMessage> Passed { get; init; }
        public required List<ChatMessage> History { get; init; }

        /// <summary>What the model gets: the stored history, then the messages passed on.</summary>
        public List<ChatMessage> ModelInput => [.. History, .. Passed];

        /// <summary>What the store holds once the run has stored the passed-on messages.</summary>
        public List<string> StoredAfterRun => [.. Store.Select(r => Describe(AIStoredMessageMapper.TryDeserialize(r.ContentJson)!)), .. Passed.Select(Describe)];
    }

    private static async Task<Run> RunAsync(List<AIMessage> store, List<ChatMessage> candidates)
    {
        var repository = new Mock<IAIConversationRepository>();
        repository
            .Setup(x => x.GetMessagesAsync(ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => store.ToList());
        repository
            .Setup(x => x.RewriteMessagesAsync(ConversationId, It.IsAny<IReadOnlyList<AIMessage>>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, IReadOnlyList<AIMessage> updates, IReadOnlyList<Guid> deleteIds, CancellationToken _) =>
            {
                store.RemoveAll(r => deleteIds.Contains(r.Id));
                foreach (var update in updates)
                {
                    store[store.FindIndex(r => r.Id == update.Id)] = update;
                }
            })
            .Returns(Task.CompletedTask);

        var provider = new ConversationChatHistoryProvider(
            repository.Object,
            Mock.Of<IAIFileStore>(),
            Mock.Of<IAIRuntimeContextAccessor>(),
            NullLogger<ConversationChatHistoryProvider>.Instance,
            Repairer(repository.Object));

        var passed = await provider.DropAlreadyPersistedLeadingMessagesAsync(ConversationId, candidates);

        var session = new TestSession();
        provider.BindConversation(session, ConversationId);
        var agent = Mock.Of<AIAgent>();
#pragma warning disable MAAI001 // InvokingContext is marked evaluation-only; it is the public way to run ProvideChatHistoryAsync.
        var model = await provider.InvokingAsync(new ChatHistoryProvider.InvokingContext(agent, session, passed));
#pragma warning restore MAAI001

        // InvokingAsync returns the stored history followed by the request messages passed in.
        var history = model.Take(model.Count() - passed.Count).ToList();

        return new Run { Repository = repository, Store = store, Passed = passed, History = history };
    }

    // Given (AC1-AC6): the store holds U, call C and the stand-in for C; the browser resends U, its copy of C and the real result.

    // AC1 - Then the model gets U once
    [Fact]
    public async Task ResentTurn_ModelGetsTheUserMessageOnce()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.ModelInput.Count(m => m.Role == ChatRole.User).ShouldBe(1);
    }

    // AC2 - Then the model gets call C once
    [Fact]
    public async Task ResentTurn_ModelGetsTheToolCallOnce()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.ModelInput.SelectMany(m => m.Contents).OfType<FunctionCallContent>().Count().ShouldBe(1);
    }

    // AC3 - Then the history the model gets holds the real result for C, not "Success: Function completed."
    [Fact]
    public async Task ResentTurn_HistoryTheModelGetsHoldsTheRealResult()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.History.Select(Describe).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC3 - And the store holds the real result
    [Fact]
    public async Task ResentTurn_StoreHoldsTheRealResult()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.Store.Select(r => Describe(AIStoredMessageMapper.TryDeserialize(r.ContentJson)!)).ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC4 - Then the conversation stores U once, call C once and one result for C
    [Fact]
    public async Task ResentTurn_AfterTheRun_StoresUserMessageCallAndResultOnce()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.StoredAfterRun.ShouldBe(["user:u1", "call:C", "result:C=real"]);
    }

    // AC5 - Given a server call S with its real result before frontend call F; then S's stored result is unchanged and F's holds the real result
    [Fact]
    public async Task ResentTurn_WithServerToolBeforeFrontendTool_UpdatesOnlyTheFrontendResult()
    {
        var store = Rows(
            User("u1"),
            Call("srv-a1", "S"),
            Result("srv-t1", ("S", "server-result")),
            Call("srv-a2", "F"),
            Result("srv-t2", ("F", StandIn)));
        List<ChatMessage> incoming =
        [
            User("u1"),
            Call("br-a", "S", "F"),
            Result("br-t", ("S", "server-result-again"), ("F", "ui-result")),
        ];

        var run = await RunAsync(store, incoming);

        run.History.Select(Describe).ShouldBe(["user:u1", "call:S", "result:S=server-result", "call:F", "result:F=ui-result"]);
    }

    // AC6 - When the resent turn is followed by a new user message N, then N is passed on to be stored
    [Fact]
    public async Task ResentTurn_FollowedByNewUserMessage_PassesTheNewMessageOn()
    {
        var run = await RunAsync(StoredTurn(), [.. ResentTurn(), User("n1", "Next question")]);

        run.StoredAfterRun.ShouldBe(["user:u1", "call:C", "result:C=real", "user:n1"]);
    }

    // The write happens once, and only when there is a real result to write

    [Fact]
    public async Task ResentTurn_WritesTheMergeInOneRewrite()
    {
        var run = await RunAsync(StoredTurn(), ResentTurn());

        run.Repository.Verify(
            x => x.RewriteMessagesAsync(ConversationId, It.Is<IReadOnlyList<AIMessage>>(u => u.Count == 1), It.Is<IReadOnlyList<Guid>>(d => d.Count == 0), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NewTurnWithDifferentUserId_DoesNotWrite()
    {
        var run = await RunAsync(StoredTurn(), [User("u2", "Hello")]);

        run.Repository.Verify(
            x => x.RewriteMessagesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<AIMessage>>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // R11 - the #375 leading-run drop still runs first, and the merge applies to what is left
    [Fact]
    public async Task LeadingRunAlreadyStored_IsStillDroppedBeforeTheMerge()
    {
        var run = await RunAsync(StoredTurn(), [User("u1"), Call("srv-a", "C"), Result("srv-t", ("C", StandIn)), User("n1", "Next")]);

        run.Passed.Select(m => m.MessageId).ShouldBe(["n1"]);
    }
}
