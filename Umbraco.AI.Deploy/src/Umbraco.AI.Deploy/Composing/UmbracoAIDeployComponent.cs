using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Composing;

/// <summary>
/// Component for registering Umbraco AI Deploy disk entity types.
/// </summary>
public class UmbracoAIDeployComponent(IDiskEntityService diskEntityService) : IAsyncComponent
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
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Settings, UdiType.GuidUdi);
    }

    private void RegisterDiskEntityTypes()
    {
        // Register disk entity types for deployment
        diskEntityService.RegisterDiskEntityType(UmbracoAIConstants.UdiEntityType.Context);
        diskEntityService.RegisterDiskEntityType(UmbracoAIConstants.UdiEntityType.Connection);
        diskEntityService.RegisterDiskEntityType(UmbracoAIConstants.UdiEntityType.Profile);
        diskEntityService.RegisterDiskEntityType(UmbracoAIConstants.UdiEntityType.Settings);
    }
}
