using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Startup.Configuration;

/// <summary>
/// Umbraco Composer for auto-discovery and registration of Umbraco.Ai.Agent services.
/// </summary>
[ComposeAfter(typeof(UmbracoAIComposer))]
public class UmbracoAIAgentComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiAgent();
    }
}
