using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Prompt.Startup.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.Prompt.Composing;

/// <summary>
/// Component for registering Umbraco AI Deploy Prompt disk entity types.
/// </summary>
public class UmbracoAIDeployPromptComponent(IDiskEntityService diskEntityService) : IAsyncComponent
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
        UdiParser.RegisterUdiType(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi);
    }

    private void RegisterDiskEntityTypes()
    {
        // Register disk entity type for deployment
        diskEntityService.RegisterDiskEntityType(UmbracoAIPromptConstants.UdiEntityType.Prompt);
    }
}
