using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// Collection of context processors. Loops items and dispatches to handlers.
/// </summary>
public sealed class AiRequestContextProcessorCollection : BuilderCollectionBase<IAiRequestContextProcessor>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiRequestContextProcessorCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the processor instances.</param>
    public AiRequestContextProcessorCollection(Func<IEnumerable<IAiRequestContextProcessor>> items)
        : base(items)
    { }

    /// <summary>
    /// Processes all context items through registered processors.
    /// </summary>
    /// <param name="items">The context items to process.</param>
    /// <returns>A populated request context with data extracted by processors.</returns>
    public AiRequestContext Process(IEnumerable<AiRequestContextItem> items)
    {
        var context = new AiRequestContext(items);

        // For each item, find processors that can handle it
        foreach (var item in context.Items)
        {
            foreach (var processor in this)
            {
                if (processor.CanHandle(item))
                {
                    processor.Process(item, context);
                    // Continue to allow multiple processors per item if needed
                }
            }
        }

        return context;
    }
}
