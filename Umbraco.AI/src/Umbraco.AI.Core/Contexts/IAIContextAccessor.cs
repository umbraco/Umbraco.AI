namespace Umbraco.AI.Core.Contexts;

/// <summary>
/// Provides access to the current AI context during tool execution.
/// </summary>
/// <remarks>
/// This is set by the context injection middleware for each AI call and cleared afterward. It is held
/// per async flow, so each call's tools see that call's context, even when calls run at the same time
/// or one runs inside another.
/// </remarks>
public interface IAIContextAccessor
{
    /// <summary>
    /// Gets the current resolved context, if any.
    /// </summary>
    AIResolvedContext? Context { get; }

    /// <summary>
    /// Sets the current resolved context.
    /// </summary>
    /// <param name="context">The resolved context to set.</param>
    /// <returns>A disposable that clears the context when disposed.</returns>
    /// <remarks>
    /// The built-in context injection keeps the default implementation's context current for every step of a
    /// streamed call. A replacement implementation (or a wrapper returning its own handle) is not re-entered
    /// between steps, so it must make its context visible to code running on later steps itself.
    /// </remarks>
    IDisposable SetContext(AIResolvedContext context);
}
