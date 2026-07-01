using Microsoft.Extensions.AI;
using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

public class MeaiApprovalRoundTripSpikeTests
{
    // A fake IChatClient that, on the FIRST call, returns one tool call to "delete_thing",
    // and on the SECOND call (after the approval response is in history) returns a plain
    // text completion. This lets us observe exactly what FICC produces/consumes.
    private sealed class ScriptedChatClient : IChatClient
    {
        private int _calls;
        public ChatClientMetadata Metadata { get; } = new("scripted");
        public bool ToolWasInvoked { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
        {
            _calls++;
            if (_calls == 1)
            {
                var call = new FunctionCallContent("call-1", "delete_thing",
                    new Dictionary<string, object?> { ["id"] = "42" });
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
            }

            // Record whether, by the second model turn, a FunctionResultContent for call-1
            // appeared in history (i.e. FICC executed the tool after approval).
            ToolWasInvoked = messages.Any(m => m.Contents.OfType<FunctionResultContent>()
                .Any(r => r.CallId == "call-1"));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    [Fact]
    public async Task ApprovalRequiredFunction_FirstTurn_ProducesApprovalRequest_AndDoesNotInvoke()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var scripted = new ScriptedChatClient();
        var client = scripted.AsBuilder().UseFunctionInvocation().Build();

        var options = new ChatOptions { Tools = [approvalFn] };
        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);

        var approvalRequest = response.Messages
            .SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault();

        approvalRequest.ShouldNotBeNull();
        approvalRequest!.ToolCall.CallId.ShouldBe("call-1");
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task ApprovalResponse_Approved_CausesToolInvocation()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var scripted = new ScriptedChatClient();
        var client = scripted.AsBuilder().UseFunctionInvocation().Build();
        var options = new ChatOptions { Tools = [approvalFn] };

        // First turn: get the approval request.
        var first = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);
        var request = first.Messages.SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>().Single();

        var approvalResponse = request.CreateResponse(approved: true, reason: null);

        var history = new List<ChatMessage> { new(ChatRole.User, "delete thing 42") };
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, [approvalResponse]));

        var second = await client.GetResponseAsync(history, options);

        invoked.ShouldBeTrue();
        scripted.ToolWasInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task ApprovalResponse_Denied_DoesNotInvoke()
    {
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var client = new ScriptedChatClient().AsBuilder().UseFunctionInvocation().Build();
        var options = new ChatOptions { Tools = [approvalFn] };

        var first = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);
        var request = first.Messages.SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>().Single();

        var denial = request.CreateResponse(approved: false, reason: "user denied");
        var history = new List<ChatMessage> { new(ChatRole.User, "delete thing 42") };
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, [denial]));

        await client.GetResponseAsync(history, options);
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task ApprovalResponse_StatelessConstruction_Approved_CausesToolInvocation()
    {
        // Tests finding C: whether a freshly built ToolApprovalResponseContent (without
        // using CreateResponse) also works — this is what the stateless resume path does.
        var invoked = false;
        var inner = AIFunctionFactory.Create(
            (string id) => { invoked = true; return $"deleted {id}"; },
            name: "delete_thing");
        var approvalFn = new ApprovalRequiredAIFunction(inner);

        var scripted = new ScriptedChatClient();
        var client = scripted.AsBuilder().UseFunctionInvocation().Build();
        var options = new ChatOptions { Tools = [approvalFn] };

        var first = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "delete thing 42")], options);
        var request = first.Messages.SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>().Single();

        // Stateless construction — what the resume path will do without the original request object.
        var statelessResponse = new ToolApprovalResponseContent(
            request.ToolCall.CallId,
            approved: true,
            request.ToolCall);

        var history = new List<ChatMessage> { new(ChatRole.User, "delete thing 42") };
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, [statelessResponse]));

        await client.GetResponseAsync(history, options);
        invoked.ShouldBeTrue();
        scripted.ToolWasInvoked.ShouldBeTrue();
    }
}
