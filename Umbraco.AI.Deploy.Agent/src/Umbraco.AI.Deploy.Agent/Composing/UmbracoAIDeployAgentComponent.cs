using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Agent.Composing;

/// <summary>
/// Component for registering Umbraco AI Deploy Agent disk entity types.
/// </summary>
public class UmbracoAIDeployAgentComponent(IDiskEntityService diskEntityService) : IAsyncComponent
{
    /// <inheritdoc />
    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        RegisterUdiTypes();
        RegisterDiskEntityTypes();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi);
    }

    private void RegisterDiskEntityTypes()
    {
        // Register disk entity type for deployment
        diskEntityService.RegisterDiskEntityType(UmbracoAIAgentConstants.UdiEntityType.Agent);
    }
}
