using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Core.Tools;

/// <summary>
/// Wrapper for frontend tools that includes metadata for permission checks.
/// </summary>
/// <remarks>
/// This class wraps AITool instances received from the frontend and adds
/// scope and destructiveness metadata needed for permission validation.
/// </remarks>
internal sealed class AIFrontendTool : AITool
{
    private readonly AITool _innerTool;

    /// <summary>
    /// Gets the scope identifier for this tool.
    /// </summary>
    public string? Scope { get; }

    /// <summary>
    /// Gets whether this tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFrontendTool"/> class.
    /// </summary>
    /// <param name="innerTool">The inner tool to wrap.</param>
    /// <param name="scope">The scope identifier for permission checks.</param>
    /// <param name="isDestructive">Whether the tool is destructive.</param>
    public AIFrontendTool(
        AITool innerTool,
        string? scope,
        bool isDestructive)
    {
        _innerTool = innerTool ?? throw new ArgumentNullException(nameof(innerTool));
        Scope = scope;
        IsDestructive = isDestructive;
    }

    /// <inheritdoc />
    public override AIToolMetadata? Metadata => _innerTool.Metadata;

    /// <inheritdoc />
    public override Task<object?> InvokeAsync(
        IEnumerable<KeyValuePair<string, object?>> arguments,
        CancellationToken cancellationToken = default)
    {
        return _innerTool.InvokeAsync(arguments, cancellationToken);
    }
}
