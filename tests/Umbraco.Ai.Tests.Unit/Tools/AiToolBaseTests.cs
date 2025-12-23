using Umbraco.Ai.Core.Tools;

namespace Umbraco.Ai.Tests.Unit.Tools;

public class AiToolBaseTests
{
    #region Constructor and attribute validation

    [Fact]
    public void Constructor_WithAttribute_SetsPropertiesFromAttribute()
    {
        // Arrange & Act
        var tool = new TestTool();

        // Assert
        tool.Id.ShouldBe("test-tool");
        tool.Name.ShouldBe("Test Tool");
        tool.Category.ShouldBe("Testing");
        tool.IsDestructive.ShouldBeTrue();
        tool.Tags.ShouldBe(new[] { "test", "fake" });
    }

    [Fact]
    public void Constructor_WithDefaultAttributeValues_UsesDefaults()
    {
        // Arrange & Act
        var tool = new MinimalTool();

        // Assert
        tool.Id.ShouldBe("minimal-tool");
        tool.Name.ShouldBe("Minimal Tool");
        tool.Category.ShouldBe("General"); // Default
        tool.IsDestructive.ShouldBeFalse(); // Default
        tool.Tags.ShouldBeEmpty(); // Default
    }

    [Fact]
    public void Constructor_WithoutAttribute_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => new ToolWithoutAttribute();

        // Assert
        var exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("missing required [AiTool] attribute");
    }

    #endregion

    #region ArgsType

    [Fact]
    public void ArgsType_UntypedTool_ReturnsNull()
    {
        // Arrange
        var tool = new TestTool();

        // Act & Assert
        tool.ArgsType.ShouldBeNull();
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_DelegatesToProtectedMethod()
    {
        // Arrange
        var tool = new TestTool();

        // Act
        var result = await ((IAiTool)tool).ExecuteAsync(null, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        var resultObj = result as dynamic;
        ((string)resultObj!.Message).ShouldBe("Executed");
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresArgsForUntypedTool()
    {
        // Arrange
        var tool = new TestTool();

        // Act - passing args to untyped tool should be ignored
        var result = await ((IAiTool)tool).ExecuteAsync(new { SomeArg = "value" }, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
    }

    #endregion

    #region Test tools

    [AiTool("test-tool", "Test Tool", Category = "Testing", IsDestructive = true, Tags = ["test", "fake"])]
    private class TestTool : AiToolBase
    {
        public override string Description => "A test tool";

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>(new { Message = "Executed" });
        }
    }

    [AiTool("minimal-tool", "Minimal Tool")]
    private class MinimalTool : AiToolBase
    {
        public override string Description => "A minimal test tool";

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>(new { });
        }
    }

    // Intentionally missing the AiToolAttribute
    private class ToolWithoutAttribute : AiToolBase
    {
        public override string Description => "Should fail";

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>(new { });
        }
    }

    #endregion
}
