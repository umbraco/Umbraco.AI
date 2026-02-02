namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// Provides access to the current <see cref="AIRuntimeContext"/>.
/// </summary>
/// <remarks>
/// Inject this interface in tools, services, and middleware that need to read from
/// or write to the runtime context. The context is only available within an active scope.
/// </remarks>
public interface IAIRuntimeContextAccessor
{
    /// <summary>
    /// Gets the current runtime context, or null if no scope is active.
    /// </summary>
    AIRuntimeContext? Context { get; }
}
