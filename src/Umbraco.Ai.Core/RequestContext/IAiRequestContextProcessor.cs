namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// Processes individual request context items.
/// Each processor handles specific item types (like entity data, user selections, etc.).
/// </summary>
public interface IAiRequestContextProcessor
{
    /// <summary>
    /// Checks if this processor can handle the given context item.
    /// </summary>
    /// <param name="item">The context item to check.</param>
    /// <returns>True if this processor can handle the item; otherwise false.</returns>
    bool CanHandle(AiRequestContextItem item);

    /// <summary>
    /// Processes a single context item and populates the context object.
    /// Only called if <see cref="CanHandle"/> returned true.
    /// </summary>
    /// <param name="item">The context item to process.</param>
    /// <param name="context">The context object to populate with extracted data.</param>
    void Process(AiRequestContextItem item, AiRequestContext context);
}
