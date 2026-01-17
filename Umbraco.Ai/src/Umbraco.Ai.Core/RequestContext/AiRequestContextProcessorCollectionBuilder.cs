using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.RequestContext;

/// <summary>
/// An ordered collection builder for AI request context processors.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure the order of context processing:
/// </para>
/// <code>
/// builder.AiRequestContextProcessors()
///     .Append&lt;SerializedEntityProcessor&gt;()
///     .Append&lt;DefaultSystemMessageProcessor&gt;();
/// </code>
/// <para>
/// Processors are executed in collection order for each context item.
/// Multiple processors can handle the same item if their <c>CanHandle</c> returns true.
/// </para>
/// </remarks>
public sealed class AiRequestContextProcessorCollectionBuilder
    : OrderedCollectionBuilderBase<AiRequestContextProcessorCollectionBuilder, AiRequestContextProcessorCollection, IAiRequestContextProcessor>
{
    /// <inheritdoc />
    protected override AiRequestContextProcessorCollectionBuilder This => this;
}
