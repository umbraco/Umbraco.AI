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
/// Scopes can be nested. Each call to <see cref="CreateScope()"/> creates a new isolated
/// context that is pushed onto a stack. When the scope is disposed, the previous context
/// is restored. This enables scenarios such as agent A calling agent B, where each agent
/// gets its own isolated context without polluting the other.
/// </para>
/// </remarks>
public interface IAiRuntimeContextScopeProvider
{
    /// <summary>
    /// Creates a new runtime context scope. The context is available via
    /// <see cref="IAiRuntimeContextAccessor.Context"/> until the scope is disposed.
    /// </summary>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    /// <remarks>
    /// If called within an existing scope, creates a new nested scope with its own
    /// isolated context. Disposing the nested scope restores the parent context.
    /// </remarks>
    IAiRuntimeContextScope CreateScope();

    /// <summary>
    /// Creates a new runtime context scope with initial context items.
    /// </summary>
    /// <param name="items">Initial context items to populate the context with.</param>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    /// <remarks>
    /// If called within an existing scope, creates a new nested scope with its own
    /// isolated context. Disposing the nested scope restores the parent context.
    /// </remarks>
    IAiRuntimeContextScope CreateScope(IEnumerable<AiRequestContextItem> items);
}
