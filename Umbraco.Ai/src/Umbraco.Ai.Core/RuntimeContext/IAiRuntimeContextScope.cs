namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Represents an active runtime context scope.
/// </summary>
/// <remarks>
/// <para>
/// Disposing the scope removes the context from the ambient state.
/// </para>
/// <para>
/// Scopes can be nested. Each nested scope has its own isolated context,
/// and disposing a nested scope restores the parent context.
/// </para>
/// </remarks>
public interface IAiRuntimeContextScope : IDisposable
{
    /// <summary>
    /// Gets the runtime context for this scope.
    /// </summary>
    AiRuntimeContext Context { get; }

    /// <summary>
    /// Gets the parent context, or <c>null</c> if this is the root scope.
    /// </summary>
    AiRuntimeContext? ParentContext { get; }

    /// <summary>
    /// Gets the nesting depth of this scope (1 for root scope, 2 for first nested, etc.).
    /// </summary>
    int Depth { get; }
}
