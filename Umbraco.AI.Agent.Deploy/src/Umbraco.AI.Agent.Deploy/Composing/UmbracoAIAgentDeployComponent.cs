using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Agent.Deploy.Composing;

/// <summary>
/// Component for registering Umbraco AI Agent Deploy UDI types.
/// </summary>
public class UmbracoAIAgentDeployComponent : IAsyncComponent
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
        UdiParser.RegisterUdiType(UmbracoAIAgentConstants.UdiEntityType.Agent, UdiType.GuidUdi);
    }
}
