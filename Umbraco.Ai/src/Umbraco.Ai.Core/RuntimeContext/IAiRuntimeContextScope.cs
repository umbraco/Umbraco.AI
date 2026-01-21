namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Represents an active runtime context scope.
/// </summary>
/// <remarks>
/// Disposing the scope removes the context from the ambient state.
/// </remarks>
public interface IAiRuntimeContextScope : IDisposable
{
    /// <summary>
    /// Gets the runtime context for this scope.
    /// </summary>
    AiRuntimeContext Context { get; }
}
