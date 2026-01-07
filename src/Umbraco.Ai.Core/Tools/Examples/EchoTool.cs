using System.ComponentModel;

namespace Umbraco.Ai.Core.Tools.Examples;

/// <summary>
/// Arguments for the Echo tool.
/// </summary>
/// <param name="Message">The message to echo.</param>
/// <param name="Repeat">Number of times to repeat (default: 1).</param>
public record EchoArgs(
    [property: Description("The message to echo")] string Message,
    [property: Description("Number of times to repeat (default: 1)")] int Repeat = 1);

/// <summary>
/// A simple example tool that echoes back the input.
/// Demonstrates the typed AiToolBase&lt;TArgs&gt; pattern.
/// </summary>
/// <remarks>
/// This tool is NOT registered by default - use it as a reference for implementing your own tools.
/// </remarks>
[AiTool("example_echo", "Echo", Category = "Example")]
public class EchoTool : AiToolBase<EchoArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Echoes back the provided message. Useful for testing tool infrastructure.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(EchoArgs args, CancellationToken cancellationToken = default)
    {
        var result = string.Join(" ", Enumerable.Repeat(args.Message, args.Repeat));
        return Task.FromResult<object>(new EchoResult(result, DateTime.UtcNow));
    }
}

/// <summary>
/// Result of the Echo tool.
/// </summary>
/// <param name="Message">The echoed message.</param>
/// <param name="Timestamp">The timestamp of execution.</param>
public record EchoResult(string Message, DateTime Timestamp);
