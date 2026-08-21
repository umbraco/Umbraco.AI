using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Defines an AI tool that can be invoked by AI models.
/// </summary>
public interface IAITool : IDiscoverable
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
    /// Gets the scope identifier for permission and grouping purposes.
    /// </summary>
    /// <remarks>
    /// Examples: "content-read", "content-write", "media-read", "search"
    /// </remarks>
    string ScopeId { get; }

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

    /// <summary>
    /// Produces a short, human-readable description of what a specific call will do, given its raw
    /// arguments (e.g. "Set 'title' to 'New Title'") -- shown to a human approving a destructive call,
    /// in addition to the tool's general <see cref="Description"/>. Returns null when the tool hasn't
    /// implemented one; callers fall back to a generic display of the raw arguments in that case. Must
    /// never perform the actual operation or any other side effect. Prefer building the description from
    /// the raw arguments alone; a synchronous, read-only lookup (e.g. resolving a parent key to its name,
    /// the same way <see cref="ConfirmationPhrase"/> resolves a target's name) is acceptable when it turns
    /// an opaque GUID into something a human can actually recognize -- as long as an unresolvable target
    /// still falls back to the raw value rather than being dropped from the description.
    /// </summary>
    /// <remarks>
    /// Default interface implementation returns null, so existing <see cref="IAITool"/> implementers
    /// (e.g. test fakes) that predate this member don't need updating to keep compiling.
    /// </remarks>
    /// <param name="args">The raw arguments for this call (a JSON element, an argument dictionary, or the tool's typed args).</param>
    string? DescribeInvocation(object? args) => null;

    /// <summary>
    /// Produces the exact phrase a human must type to unlock the Approve button for this specific call,
    /// for destructive calls that warrant more friction than a plain click (e.g. publishing or deleting
    /// a content item) -- typically the target item's display name. Returns null (the default) for
    /// ordinary destructive calls, which keep the plain Approve/Deny buttons with no typed confirmation.
    /// Unlike <see cref="DescribeInvocation"/>, this MAY perform a lookup (e.g. resolving a content key
    /// to its name): it runs once, synchronously, while the approval interrupt is built -- not on every
    /// render.
    /// </summary>
    /// <remarks>
    /// Default interface implementation returns null, so existing <see cref="IAITool"/> implementers
    /// (e.g. test fakes) that predate this member don't need updating to keep compiling.
    /// </remarks>
    /// <param name="args">The raw arguments for this call (a JSON element, an argument dictionary, or the tool's typed args).</param>
    string? ConfirmationPhrase(object? args) => null;
}
