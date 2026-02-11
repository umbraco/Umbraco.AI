using System.Reflection;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Base class for AI tools, providing common metadata.
/// </summary>
public abstract class AIToolBasic
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
    /// Gets the scope identifier for permission and grouping purposes.
    /// </summary>
    public string ScopeId { get; }

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
    /// Initializes a new instance of the <see cref="AIToolBase"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the tool is missing the required attribute.</exception>
    protected AIToolBasic()
    {
        var attribute = GetType().GetCustomAttribute<AIToolAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Tool '{GetType().FullName}' is missing required [AITool] attribute.");

        Id = attribute.Id;
        Name = attribute.Name;
        ScopeId = attribute.ScopeId;
        IsDestructive = attribute.IsDestructive;
        Tags = attribute.Tags;
    }
}

/// <summary>
/// Base class for AI tools that don't require arguments.
/// </summary>
public abstract class AIToolBase : AIToolBasic, IAITool
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
    Task<object> IAITool.ExecuteAsync(object? args, CancellationToken cancellationToken)
        => ExecuteAsync(cancellationToken);
}

/// <summary>
/// Base class for AI tools with strongly-typed arguments.
/// </summary>
/// <typeparam name="TArgs">The arguments model type. Use records with [Description] attributes on properties.</typeparam>
public abstract class AIToolBase<TArgs> : AIToolBasic, IAITool
    where TArgs : class
{
    /// <inheritdoc />
    public override Type ArgsType => typeof(TArgs);

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
    Task<object> IAITool.ExecuteAsync(object? args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                var deserializedArgs = System.Text.Json.JsonSerializer.Deserialize<TArgs>(jsonElement, Constants.DefaultJsonSerializerOptions);
                if (deserializedArgs is null)
                {
                    throw new ArgumentException(
                        $"Failed to deserialize arguments to {typeof(TArgs).Name}. " +
                        $"JSON: {jsonElement.GetRawText()}");
                }

                return ExecuteAsync(deserializedArgs, cancellationToken);
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ArgumentException(
                    $"Invalid arguments for tool '{Id}'. " +
                    $"Expected type: {typeof(TArgs).Name}. " +
                    $"JSON: {jsonElement.GetRawText()}. " +
                    $"Error: {ex.Message}",
                    ex);
            }
        }

        if (args is TArgs typedArgs)
        {
            return ExecuteAsync(typedArgs, cancellationToken);
        }

        throw new ArgumentException(
            $"Tool '{Id}' received arguments of unexpected type {args.GetType().Name}. " +
            $"Expected {typeof(TArgs).Name} or JsonElement. " +
            $"Value: {System.Text.Json.JsonSerializer.Serialize(args)}");
    }
}
