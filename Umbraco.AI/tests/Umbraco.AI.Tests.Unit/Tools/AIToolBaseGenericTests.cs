using System.ComponentModel;
using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Tests.Unit.Tools;

public class AIToolBaseGenericTests
{
    #region ArgsType

    [Fact]
    public void ArgsType_TypedTool_ReturnsTypeOfTArgs()
    {
        // Arrange
        var tool = new TypedTestTool();

        // Act & Assert
        tool.ArgsType.ShouldBe(typeof(TestArgs));
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_WithValidArgs_DelegatesToTypedMethod()
    {
        // Arrange
        var tool = new TypedTestTool();
        var args = new TestArgs("Hello", 5);

        // Act
        var result = await ((IAITool)tool).ExecuteAsync(args, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        var resultObj = result as TestResult;
        resultObj.ShouldNotBeNull();
        resultObj!.Echo.ShouldBe("Hello Hello Hello Hello Hello");
        resultObj.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var tool = new TypedTestTool();

        // Act
        var act = () => ((IAITool)tool).ExecuteAsync(null, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongArgsType_ThrowsArgumentException()
    {
        // Arrange
        var tool = new TypedTestTool();
        var wrongArgs = new { Message = "wrong type" };

        // Act
        var act = () => ((IAITool)tool).ExecuteAsync(wrongArgs, CancellationToken.None);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    #endregion

    #region Test types

    public record TestArgs(
        [property: Description("The message")] string Message,
        [property: Description("The count")] int Count = 1);

    public record TestResult(string Echo, int Count);

    [AITool("typed-test-tool", "Typed Test Tool", ScopeId = "Testing")]
    private class TypedTestTool : AIToolBase<TestArgs>
    {
        public override string Description => "A typed test tool";

        protected override Task<object> ExecuteAsync(TestArgs args, CancellationToken cancellationToken = default)
        {
            var echo = string.Join(" ", Enumerable.Repeat(args.Message, args.Count));
            return Task.FromResult<object>(new TestResult(echo, args.Count));
        }
    }

    #endregion
}
