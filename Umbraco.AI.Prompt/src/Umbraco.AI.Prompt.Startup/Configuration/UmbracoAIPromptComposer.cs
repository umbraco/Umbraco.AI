using Umbraco.AI.Prompt.Extensions;
using Umbraco.AI.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Prompt.Startup.Configuration;

/// <summary>
/// Umbraco Composer for auto-discovery and registration of Umbraco.Ai.Prompt services.
/// </summary>
[ComposeAfter(typeof(UmbracoAIComposer))]
public class UmbracoAIPromptComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiPrompt();
    }
}
