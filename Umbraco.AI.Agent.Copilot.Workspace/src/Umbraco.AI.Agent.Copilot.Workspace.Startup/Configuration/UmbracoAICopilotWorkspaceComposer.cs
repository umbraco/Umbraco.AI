using Umbraco.AI.Agent.Copilot.Workspace.Extensions;
using Umbraco.AI.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Copilot.Workspace.Startup.Configuration;

/// <summary>
/// Umbraco Composer for auto-discovery and registration of Umbraco.AI.Agent.Copilot.Workspace services.
/// </summary>
[ComposeAfter(typeof(UmbracoAIComposer))]
public class UmbracoAICopilotWorkspaceComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddUmbracoAICopilotWorkspace();
    }
}
