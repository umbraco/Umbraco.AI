using Shouldly;
using Umbraco.AI.Agui.Events.Lifecycle;
using Umbraco.AI.Agui.Events.Messages;
using Umbraco.AI.Agui.Events.Tools;
using Umbraco.AI.Agui.Models;
using Umbraco.AI.Agui.Streaming;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agui;

public class AguiEventEmitterTests
{
    private const string TestThreadId = "thread-123";
    private const string TestRunId = "run-456";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidIds_SetsProperties()
    {
        // Act
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Assert
        emitter.ThreadId.ShouldBe(TestThreadId);
        emitter.RunId.ShouldBe(TestRunId);
        emitter.CurrentMessageId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyThreadId_GeneratesNewThreadId()
    {
        // Act
        var emitter = new AguiEventEmitter(string.Empty, TestRunId);

        // Assert
        emitter.ThreadId.ShouldNotBeNullOrEmpty();
        emitter.ThreadId.ShouldNotBe(string.Empty);
    }

    [Fact]
    public void Constructor_WithEmptyRunId_GeneratesNewRunId()
    {
        // Act
        var emitter = new AguiEventEmitter(TestThreadId, string.Empty);

        // Assert
        emitter.RunId.ShouldNotBeNullOrEmpty();
        emitter.RunId.ShouldNotBe(string.Empty);
    }

    #endregion

    #region EmitRunStarted Tests

    [Fact]
    public void EmitRunStarted_ReturnsEventWithCorrectIds()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitRunStarted();

        // Assert
        evt.ShouldBeOfType<RunStartedEvent>();
        evt.ThreadId.ShouldBe(TestThreadId);
        evt.RunId.ShouldBe(TestRunId);
        evt.Timestamp.ShouldNotBeNull();
        evt.Timestamp.Value.ShouldBeGreaterThan(0);
    }

    #endregion

    #region EmitTextChunk Tests

    [Fact]
    public void EmitTextChunk_ReturnsEventWithDelta()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        var delta = "Hello, world!";

        // Act
        var evt = emitter.EmitTextChunk(delta);

        // Assert
        evt.ShouldBeOfType<TextMessageChunkEvent>();
        evt.Delta.ShouldBe(delta);
        evt.Role.ShouldBe(AguiMessageRole.Assistant);
        evt.MessageId.ShouldBe(emitter.CurrentMessageId);
    }

    [Fact]
    public void EmitTextChunk_MultipleChunks_UseSameMessageId()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt1 = emitter.EmitTextChunk("First ");
        var evt2 = emitter.EmitTextChunk("Second ");
        var evt3 = emitter.EmitTextChunk("Third");

        // Assert
        evt1.MessageId.ShouldBe(evt2.MessageId);
        evt2.MessageId.ShouldBe(evt3.MessageId);
    }

    #endregion

    #region EmitToolCall Tests

    [Fact]
    public void EmitToolCall_ReturnsEventWithDetails()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        var args = new Dictionary<string, object?> { ["city"] = "London" };

        // Act
        var evt = emitter.EmitToolCall("call-123", "get_weather", args, isFrontendTool: false);

        // Assert
        evt.ShouldNotBeNull();
        evt.ShouldBeOfType<ToolCallChunkEvent>();
        evt.ToolCallId.ShouldBe("call-123");
        evt.ToolCallName.ShouldBe("get_weather");
        evt.ParentMessageId.ShouldBe(emitter.CurrentMessageId);
        evt.Delta.ShouldContain("London");
    }

    [Fact]
    public void EmitToolCall_FrontendTool_TracksFrontendToolCall()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        emitter.EmitToolCall("call-frontend", "confirm", null, isFrontendTool: true);

        // Assert
        emitter.HasFrontendToolCalls.ShouldBeTrue();
        emitter.IsFrontendToolCall("call-frontend").ShouldBeTrue();
        emitter.FrontendToolCallIds.ShouldContain("call-frontend");
    }

    [Fact]
    public void EmitToolCall_BackendTool_DoesNotTrackAsFrontend()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        emitter.EmitToolCall("call-backend", "search", null, isFrontendTool: false);

        // Assert
        emitter.HasFrontendToolCalls.ShouldBeFalse();
        emitter.IsFrontendToolCall("call-backend").ShouldBeFalse();
    }

    [Fact]
    public void EmitToolCall_DuplicateToolCallId_ReturnsNull()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-dup", "tool1", null, false);

        // Act
        var evt = emitter.EmitToolCall("call-dup", "tool1", null, false);

        // Assert
        evt.ShouldBeNull();
    }

    [Fact]
    public void EmitToolCall_EmptyToolCallId_ReturnsNull()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitToolCall(string.Empty, "tool", null, false);

        // Assert
        evt.ShouldBeNull();
    }

    [Fact]
    public void EmitToolCall_NullArguments_EmitsEmptyJsonObject()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitToolCall("call-123", "tool", null, false);

        // Assert
        evt.ShouldNotBeNull();
        evt.Delta.ShouldBe("{}");
    }

    #endregion

    #region EmitToolResult Tests

    [Fact]
    public void EmitToolResult_BackendTool_ReturnsEvent()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-backend", "search", null, isFrontendTool: false);
        var result = new { data = "search results" };

        // Act
        var evt = emitter.EmitToolResult("call-backend", result);

        // Assert
        evt.ShouldNotBeNull();
        evt.ShouldBeOfType<ToolCallResultEvent>();
        evt.ToolCallId.ShouldBe("call-backend");
        evt.Content.ShouldContain("search results");
        evt.Role.ShouldBe(AguiMessageRole.Tool);
    }

    [Fact]
    public void EmitToolResult_FrontendTool_ReturnsNull()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-frontend", "confirm", null, isFrontendTool: true);

        // Act
        var evt = emitter.EmitToolResult("call-frontend", new { approved = true });

        // Assert
        evt.ShouldBeNull();
    }

    [Fact]
    public void EmitToolResult_EmptyToolCallId_ReturnsNull()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitToolResult(string.Empty, "result");

        // Assert
        evt.ShouldBeNull();
    }

    [Fact]
    public void EmitToolResult_RegeneratesMessageId()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-1", "tool", null, false);
        var originalMessageId = emitter.CurrentMessageId;

        // Act
        emitter.EmitToolResult("call-1", "result");

        // Assert
        emitter.CurrentMessageId.ShouldNotBe(originalMessageId);
    }

    [Fact]
    public void EmitToolResult_NullResult_EmitsNullString()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-1", "tool", null, false);

        // Act
        var evt = emitter.EmitToolResult("call-1", null);

        // Assert
        evt.ShouldNotBeNull();
        evt.Content.ShouldBe("null");
    }

    #endregion

    #region EmitError Tests

    [Fact]
    public void EmitError_ReturnsErrorEvent()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitError("Something went wrong", "ERR_001");

        // Assert
        evt.ShouldBeOfType<RunErrorEvent>();
        evt.Message.ShouldBe("Something went wrong");
        evt.Code.ShouldBe("ERR_001");
    }

    [Fact]
    public void EmitError_WithoutCode_SetsNullCode()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitError("Error message");

        // Assert
        evt.Code.ShouldBeNull();
    }

    #endregion

    #region EmitRunFinished Tests

    [Fact]
    public void EmitRunFinished_NoError_NoFrontendTools_ReturnsSuccess()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);

        // Act
        var evt = emitter.EmitRunFinished();

        // Assert
        evt.ShouldBeOfType<RunFinishedEvent>();
        evt.ThreadId.ShouldBe(TestThreadId);
        evt.RunId.ShouldBe(TestRunId);
        evt.Outcome.ShouldBe(AguiRunOutcome.Success);
        evt.Interrupt.ShouldBeNull();
    }

    [Fact]
    public void EmitRunFinished_WithFrontendTools_ReturnsInterrupt()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-frontend", "confirm", null, isFrontendTool: true);

        // Act
        var evt = emitter.EmitRunFinished();

        // Assert
        evt.Outcome.ShouldBe(AguiRunOutcome.Interrupt);
        evt.Interrupt.ShouldNotBeNull();
        evt.Interrupt.Reason.ShouldBe("tool_execution");
        evt.Interrupt.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void EmitRunFinished_WithError_ReturnsError()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        var error = new Exception("Test error");

        // Act
        var evt = emitter.EmitRunFinished(error);

        // Assert
        evt.Outcome.ShouldBe(AguiRunOutcome.Error);
        evt.Interrupt.ShouldBeNull();
    }

    [Fact]
    public void EmitRunFinished_WithErrorAndFrontendTools_ErrorTakesPrecedence()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-frontend", "confirm", null, isFrontendTool: true);
        var error = new Exception("Test error");

        // Act
        var evt = emitter.EmitRunFinished(error);

        // Assert
        evt.Outcome.ShouldBe(AguiRunOutcome.Error);
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void HasEmittedToolCall_AfterEmit_ReturnsTrue()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        emitter.EmitToolCall("call-123", "tool", null, false);

        // Act & Assert
        emitter.HasEmittedToolCall("call-123").ShouldBeTrue();
        emitter.HasEmittedToolCall("call-other").ShouldBeFalse();
    }

    [Fact]
    public void RegenerateMessageId_ChangesMessageId()
    {
        // Arrange
        var emitter = new AguiEventEmitter(TestThreadId, TestRunId);
        var original = emitter.CurrentMessageId;

        // Act
        emitter.RegenerateMessageId();

        // Assert
        emitter.CurrentMessageId.ShouldNotBe(original);
    }

    #endregion
}
