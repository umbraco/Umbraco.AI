using Umbraco.Ai.Agent.Extensions;
using Umbraco.Ai.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Agent.Startup.Configuration;

/// <summary>
/// Umbraco Composer for auto-discovery and registration of Umbraco.Ai.Agent services.
/// </summary>
[ComposeAfter(typeof(UmbracoAiComposer))]
public class UmbracoAiAgentComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAiAgent();
    }
}
