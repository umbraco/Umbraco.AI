namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// Contributes data to the runtime context from individual context items.
/// Each contributor handles specific item types (like entity data, user selections, etc.).
/// </summary>
public interface IAiRuntimeContextContributor
{
    /// <summary>
    /// Checks if this contributor can handle the given context item.
    /// </summary>
    /// <param name="item">The context item to check.</param>
    /// <returns>True if this contributor can handle the item; otherwise false.</returns>
    bool CanHandle(AiRequestContextItem item);

    /// <summary>
    /// Contributes data from a single context item to the runtime context.
    /// Only called if <see cref="CanHandle"/> returned true.
    /// </summary>
    /// <param name="item">The context item to process.</param>
    /// <param name="context">The runtime context to populate with extracted data.</param>
    void Contribute(AiRequestContextItem item, AiRuntimeContext context);
}
