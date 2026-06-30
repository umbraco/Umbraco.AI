using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIInterruptKindTests
{
    [Fact]
    public void ForApproval_ReturnsApprovalPrefixedId()
    {
        AGUIInterruptKind.ForApproval("call-123").ShouldBe("approval:call-123");
    }

    [Fact]
    public void IsApproval_WithApprovalPrefixedId_ReturnsTrue()
    {
        AGUIInterruptKind.IsApproval("approval:call-123").ShouldBeTrue();
    }

    [Fact]
    public void IsApproval_WithNonApprovalId_ReturnsFalse()
    {
        AGUIInterruptKind.IsApproval("call-123").ShouldBeFalse();
        AGUIInterruptKind.IsApproval("tool_call:call-123").ShouldBeFalse();
        AGUIInterruptKind.IsApproval(string.Empty).ShouldBeFalse();
    }

    [Fact]
    public void GetCallId_WithApprovalId_ReturnsCallId()
    {
        AGUIInterruptKind.GetCallId("approval:call-123").ShouldBe("call-123");
    }

    [Fact]
    public void GetCallId_WithNonApprovalId_ReturnsNull()
    {
        AGUIInterruptKind.GetCallId("call-123").ShouldBeNull();
    }

    [Theory]
    [InlineData("approval:")]
    [InlineData("approval:call-abc-xyz")]
    public void ForApproval_RoundTrips_Correctly(string interruptId)
    {
        var callId = AGUIInterruptKind.GetCallId(interruptId)!;
        AGUIInterruptKind.ForApproval(callId).ShouldBe(interruptId);
        AGUIInterruptKind.IsApproval(interruptId).ShouldBeTrue();
    }
}
