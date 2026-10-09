namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Creates runtime context scopes for AI operations.
/// </summary>
/// <remarks>
/// <para>
/// Inject this interface in orchestrators (like <c>AGUIStreamingService</c>) that need to
/// create a scope for the duration of an AI operation. The context is available via
/// <see cref="IAIRuntimeContextAccessor"/> until the scope is disposed.
/// </para>
/// <para>
/// Scopes can be nested. Each call to <see cref="CreateScope()"/> creates a new isolated
/// context and makes it current. When the scope is disposed, the previous context
/// is restored. This enables scenarios such as agent A calling agent B, where each agent
/// gets its own isolated context without polluting the other.
/// </para>
/// <para>
/// The current context belongs to the async flow that created the scope: the code that created it and
/// everything it awaits or starts. Work running in parallel (e.g. two AI calls started together) each
/// sees its own context. A scope created inside an async method or iterator is not current for that
/// method's caller, nor for the iterator's later steps (they run in the consumer's flow). Create the scope
/// in the method that makes the AI call, around the call; the streaming APIs keep the scope current for
/// every update of a stream started while it is current.
/// </para>
/// </remarks>
public interface IAIRuntimeContextScopeProvider
{
    /// <summary>
    /// Creates a new runtime context scope. The context is available via
    /// <see cref="IAIRuntimeContextAccessor.Context"/> until the scope is disposed.
    /// </summary>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    /// <remarks>
    /// If called within an existing scope, creates a new nested scope with its own
    /// isolated context. Disposing the nested scope restores the parent context.
    /// </remarks>
    IAIRuntimeContextScope CreateScope();

    /// <summary>
    /// Creates a new runtime context scope with initial context items.
    /// </summary>
    /// <param name="items">Initial context items to populate the context with.</param>
    /// <returns>A disposable scope that owns the runtime context.</returns>
    /// <remarks>
    /// If called within an existing scope, creates a new nested scope with its own
    /// isolated context. Disposing the nested scope restores the parent context.
    /// </remarks>
    IAIRuntimeContextScope CreateScope(IEnumerable<AIRequestContextItem> items);
}
