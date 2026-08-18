using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// An ordered collection builder for AI file thread lifecycle providers.
/// </summary>
/// <remarks>
/// <para>
/// Always registered by <c>AddUmbracoAIAgentCore</c>, even with zero providers, so the file store can
/// resolve the collection whether or not anything registers into it. Example:
/// </para>
/// <code>
/// builder.AIFileThreadLifecycleProviders()
///     .Append&lt;ConversationFileThreadLifecycleProvider&gt;();
/// </code>
/// </remarks>
public sealed class AIFileThreadLifecycleProviderCollectionBuilder
    : OrderedCollectionBuilderBase<AIFileThreadLifecycleProviderCollectionBuilder, AIFileThreadLifecycleProviderCollection, IAIFileThreadLifecycleProvider>
{
    /// <inheritdoc />
    protected override AIFileThreadLifecycleProviderCollectionBuilder This => this;
}
