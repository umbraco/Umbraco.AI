using System.Threading;
using System.Threading.Tasks;
using Umbraco.AI.Prompt.Startup.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Prompt.Deploy.Composing;

/// <summary>
/// Component for registering Umbraco AI Prompt Deploy UDI types.
/// </summary>
public class UmbracoAIPromptDeployComponent : IAsyncComponent
{
    /// <inheritdoc />
    public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        RegisterUdiTypes();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    private static void RegisterUdiTypes()
    {
        UdiParser.RegisterUdiType(UmbracoAIPromptConstants.UdiEntityType.Prompt, UdiType.GuidUdi);
    }
}
