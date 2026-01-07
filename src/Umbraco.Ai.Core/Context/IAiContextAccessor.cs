namespace Umbraco.Ai.Core.Context;

/// <summary>
/// Provides access to the current AI context during tool execution.
/// </summary>
/// <remarks>
/// This is set by the context injection middleware before tool execution
/// and cleared afterward. Uses AsyncLocal for thread-safety.
/// </remarks>
public interface IAiContextAccessor
{
    /// <summary>
    /// Gets the current resolved context, if any.
    /// </summary>
    AiResolvedContext? Context { get; }

    /// <summary>
    /// Sets the current resolved context.
    /// </summary>
    /// <param name="context">The resolved context to set.</param>
    /// <returns>A disposable that clears the context when disposed.</returns>
    IDisposable SetContext(AiResolvedContext context);
}
