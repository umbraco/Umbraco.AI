using System.Reflection;

namespace Umbraco.Ai.Core.Tools;

/// <summary>
/// Base class for AI tools, providing common metadata.
/// </summary>
public abstract class AiToolBasic
{
    /// <summary>
    /// Gets the unique identifier of the tool.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the tool.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the category of the tool for grouping purposes.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets whether the tool performs destructive operations.
    /// </summary>
    public bool IsDestructive { get; }

    /// <summary>
    /// Gets tags for additional categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the arguments model type, if the tool uses strongly-typed args.
    /// </summary>
    public virtual Type? ArgsType => null;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AiToolBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the tool is missing the required attribute.</exception>
    protected AiToolBasic()
    {
        var attribute = GetType().GetCustomAttribute<AiToolAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Tool '{GetType().FullName}' is missing required [AiTool] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
        Category = attribute.Category;
        IsDestructive = attribute.IsDestructive;
        Tags = attribute.Tags;
    }
}

/// <summary>
/// Base class for AI tools that don't require arguments.
/// </summary>
public abstract class AiToolBase : AiToolBasic, IAiTool
{
    /// <summary>
    /// Executes the tool. Override this method to implement the tool's logic.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    protected abstract Task<object> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicit interface implementation - delegates to the parameterless abstract method.
    /// </summary>
    Task<object> IAiTool.ExecuteAsync(object? args, CancellationToken cancellationToken)
        => ExecuteAsync(cancellationToken);
}

/// <summary>
/// Base class for AI tools with strongly-typed arguments.
/// </summary>
/// <typeparam name="TArgs">The arguments model type. Use records with [Description] attributes on properties.</typeparam>
public abstract class AiToolBase<TArgs> : AiToolBasic, IAiTool
    where TArgs : class
{
    /// <summary>
    /// Executes the tool with strongly-typed arguments.
    /// Override this method to implement the tool's logic.
    /// </summary>
    /// <param name="args">The strongly-typed arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    protected abstract Task<object> ExecuteAsync(TArgs args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicit interface implementation - casts args and delegates to typed method.
    /// </summary>
    Task<object> IAiTool.ExecuteAsync(object? args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        return ExecuteAsync((TArgs)args, cancellationToken);
    }
}
