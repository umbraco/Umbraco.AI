using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Tools;

/// <summary>
/// Defines an AI tool that can be invoked by AI models.
/// </summary>
public interface IAiTool : IDiscoverable
{
    /// <summary>
    /// Gets the unique identifier of the tool.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the category of the tool for grouping purposes.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets whether the tool performs destructive operations.
    /// </summary>
    bool IsDestructive { get; }

    /// <summary>
    /// Gets tags for additional categorization.
    /// </summary>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the arguments model type, if the tool uses strongly-typed args.
    /// </summary>
    Type? ArgsType { get; }

    /// <summary>
    /// Executes the tool with the provided arguments.
    /// </summary>
    /// <param name="args">The arguments for the tool, or null for tools without arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<object> ExecuteAsync(object? args, CancellationToken cancellationToken = default);
}
