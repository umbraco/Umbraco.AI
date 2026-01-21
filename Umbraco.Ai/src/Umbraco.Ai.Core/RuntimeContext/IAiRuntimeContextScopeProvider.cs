namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Creates runtime context scopes for AI operations.
/// </summary>
/// <remarks>
/// <para>
/// Inject this interface in orchestrators (like <c>AguiStreamingService</c>) that need to
/// create a scope for the duration of an AI operation. The context is available via
/// <see cref="IAiRuntimeContextAccessor"/> until the scope is disposed.
/// </para>
/// <para>
/// Only one scope should be active per request. If <see cref="CreateScope"/> is called
/// while a scope already exists, it returns a no-op wrapper around the existing scope
/// to support nested usage patterns.
/// </para>
/// </remarks>
public interface IAiRuntimeContextScopeProvider
{
    /// <summary>
    /// Creates a new runtime context scope. The context is available via
    /// <see cref="IAiRuntimeContextAccessor.Context"/> until the scope is disposed.
    /// </summary>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    IAiRuntimeContextScope CreateScope();

    /// <summary>
    /// Creates a new runtime context scope with initial context items.
    /// </summary>
    /// <param name="items">Initial context items to populate the context with.</param>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    IAiRuntimeContextScope CreateScope(IEnumerable<AiRuntimeContextItem> items);
}
