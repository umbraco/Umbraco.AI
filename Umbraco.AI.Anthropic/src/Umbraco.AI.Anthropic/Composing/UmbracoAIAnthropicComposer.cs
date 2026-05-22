using Umbraco.AI.Anthropic.Errors;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Anthropic.Composing;

/// <summary>
/// Registers Umbraco.AI.Anthropic services that aren't picked up by provider auto-discovery.
/// </summary>
public class UmbracoAIAnthropicComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Insert the Anthropic-specific classifier before the generic ClientModel fallback so it
        // gets first shot at Anthropic SDK exceptions (in particular, the mid-stream SSE error
        // envelope that motivated this work — see issue #174).
        builder.AIProviderErrorClassifiers()
            .InsertBefore<ClientModelProviderErrorClassifier, AnthropicProviderErrorClassifier>();
    }
}
