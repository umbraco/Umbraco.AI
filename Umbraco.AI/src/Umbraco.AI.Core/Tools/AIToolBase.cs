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
                var deserializedArgs = TryConvertDirectOrJson(jsonElement);
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

        // Deliberately does NOT fall back to the lenient serialize-round-trip conversion the
        // approval-UI hooks below use: an unexpected argument shape reaching actual execution is a
        // caller bug worth surfacing loudly, not something to paper over.
        if (TryConvertDirectOrJson(args) is { } typedArgs)
        {
            return ExecuteAsync(typedArgs, cancellationToken);
        }

        throw new ArgumentException(
            $"Tool '{Id}' received arguments of unexpected type {args.GetType().Name}. " +
            $"Expected {typeof(TArgs).Name} or JsonElement. " +
            $"Value: {System.Text.Json.JsonSerializer.Serialize(args)}");
    }

    /// <summary>
    /// Produces a short, human-readable description of what this specific call will do, given its
    /// strongly-typed arguments. Override for destructive tools where the raw argument values alone
    /// aren't clear on their own (e.g. a bare content key, or a property path with no context). Returns
    /// null by default, which falls back to a generic argument-by-argument display in the approval UI.
    /// Override <see cref="DescribeInvocationAsync(TArgs)"/> instead if producing the description needs
    /// an asynchronous lookup — only one of the two overloads needs overriding.
    /// </summary>
    /// <param name="args">The strongly-typed arguments for this call.</param>
    protected virtual string? DescribeInvocation(TArgs args) => null;

    /// <summary>
    /// Async counterpart to <see cref="DescribeInvocation(TArgs)"/> — override this instead when the
    /// description needs an asynchronous lookup (e.g. resolving a key via Umbraco's async content/media
    /// APIs) to be meaningful. Defaults to wrapping <see cref="DescribeInvocation(TArgs)"/>'s result, so
    /// a tool with nothing to look up only needs to override the sync overload above.
    /// </summary>
    /// <param name="args">The strongly-typed arguments for this call.</param>
    protected virtual Task<string?> DescribeInvocationAsync(TArgs args) => Task.FromResult(DescribeInvocation(args));

    /// <summary>
    /// Produces the exact phrase a human must type to unlock the Approve button for this specific call.
    /// Override for calls that warrant more friction than a plain click (e.g. publishing or deleting a
    /// content item) -- typically by looking up the target item and returning its display name. Returns
    /// null by default, which keeps the plain Approve/Deny buttons with no typed confirmation. Override
    /// <see cref="ResolveConfirmationPhraseAsync(TArgs)"/> instead if the lookup needs to be asynchronous
    /// — only one of the two overloads needs overriding.
    /// </summary>
    /// <param name="args">The strongly-typed arguments for this call.</param>
    protected virtual string? ResolveConfirmationPhrase(TArgs args) => null;

    /// <summary>
    /// Async counterpart to <see cref="ResolveConfirmationPhrase(TArgs)"/> — override this instead when
    /// the lookup needs to be asynchronous (e.g. Umbraco's async content/media APIs). Defaults to
    /// wrapping <see cref="ResolveConfirmationPhrase(TArgs)"/>'s result, so a tool with a synchronous (or
    /// no) lookup only needs to override the sync overload above.
    /// </summary>
    /// <param name="args">The strongly-typed arguments for this call.</param>
    protected virtual Task<string?> ResolveConfirmationPhraseAsync(TArgs args) => Task.FromResult(ResolveConfirmationPhrase(args));

    /// <summary>
    /// Explicit interface implementation - deserializes args to <typeparamref name="TArgs"/> and
    /// delegates to <see cref="DescribeInvocationAsync(TArgs)"/> (which falls back to the sync
    /// <see cref="DescribeInvocation(TArgs)"/> unless a tool overrides the async overload directly).
    /// Never throws: description generation must not break the approval flow, so any deserialization or
    /// generation failure falls back to null.
    /// </summary>
    async Task<string?> IAITool.DescribeInvocationAsync(object? args)
    {
        if (TryDeserializeArgs(args) is not { } typedArgs)
        {
            return null;
        }

        try
        {
            return await DescribeInvocationAsync(typedArgs);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Explicit interface implementation - deserializes args to <typeparamref name="TArgs"/> and
    /// delegates to <see cref="ResolveConfirmationPhraseAsync(TArgs)"/> (which falls back to the sync
    /// <see cref="ResolveConfirmationPhrase(TArgs)"/> unless a tool overrides the async overload
    /// directly). Never throws: a lookup failure falls back to null (no confirmation gate) rather than
    /// blocking the approval flow.
    /// </summary>
    async Task<string?> IAITool.ResolveConfirmationPhraseAsync(object? args)
    {
        if (TryDeserializeArgs(args) is not { } typedArgs)
        {
            return null;
        }

        try
        {
            return await ResolveConfirmationPhraseAsync(typedArgs);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes raw call arguments (a <typeparamref name="TArgs"/> instance, a JSON element, or an
    /// arbitrary object) to <typeparamref name="TArgs"/>, or null on any failure -- shared by the
    /// approval-UI hooks above, both of which must degrade gracefully rather than throw. Unlike
    /// <see cref="TryConvertDirectOrJson"/> (used by actual execution), this also falls back to a
    /// serialize-round-trip for an arbitrary object shape, since a best-effort UI hint failing outright
    /// on an unusual-but-convertible shape would be a worse outcome than the fallback conversion.
    /// </summary>
    private static TArgs? TryDeserializeArgs(object? args)
    {
        if (args is null)
        {
            return null;
        }

        try
        {
            return TryConvertDirectOrJson(args) ?? System.Text.Json.JsonSerializer.Deserialize<TArgs>(
                System.Text.Json.JsonSerializer.SerializeToElement(args, Constants.DefaultJsonSerializerOptions),
                Constants.DefaultJsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts raw call arguments to <typeparamref name="TArgs"/> via the two "cheap" shapes: an
    /// already-typed instance, or a JSON element to deserialize. Returns null for any other shape --
    /// shared by <see cref="IAITool.ExecuteAsync"/> and <see cref="TryDeserializeArgs"/>, which then
    /// apply their own, deliberately different, policy for what to do with an unrecognized shape (throw
    /// vs. attempt a lenient fallback).
    /// </summary>
    private static TArgs? TryConvertDirectOrJson(object? args) => args switch
    {
        TArgs t => t,
        System.Text.Json.JsonElement jsonElement =>
            System.Text.Json.JsonSerializer.Deserialize<TArgs>(jsonElement, Constants.DefaultJsonSerializerOptions),
        _ => null,
    };
}
