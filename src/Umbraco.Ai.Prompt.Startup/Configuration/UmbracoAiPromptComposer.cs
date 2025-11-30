using Umbraco.Ai.Prompt.Extensions;
using Umbraco.Ai.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Prompt.Startup.Configuration;

/// <summary>
/// Umbraco Composer for auto-discovery and registration of Umbraco.Ai.Prompt services.
/// </summary>
[ComposeAfter(typeof(UmbracoAiComposer))]
public class UmbracoAiPromptComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiPrompt();
    }
}
