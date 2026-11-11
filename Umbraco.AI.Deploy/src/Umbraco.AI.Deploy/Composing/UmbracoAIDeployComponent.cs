using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Deploy.Composing;

/// <summary>
/// Component for registering Umbraco AI Deploy UDI types.
/// </summary>
public class UmbracoAIDeployComponent : IAsyncComponent
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
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Connection, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Guardrail, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Profile, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(UmbracoAIConstants.UdiEntityType.Settings, UdiType.GuidUdi);
    }
}
