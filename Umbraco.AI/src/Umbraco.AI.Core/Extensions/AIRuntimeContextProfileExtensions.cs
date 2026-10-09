using Umbraco.AI.Core;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Writes a profile's identity into an <see cref="AIRuntimeContext"/>.
/// </summary>
internal static class AIRuntimeContextProfileExtensions
{
    /// <summary>
    /// Sets the profile ID, alias and version and the provider and model the profile uses, so tracking,
    /// audit and telemetry can attribute the call to it.
    /// </summary>
    public static void SetProfileMetadata(this AIRuntimeContext context, AIProfile profile)
    {
        context.SetValue(Constants.ContextKeys.ProfileId, profile.Id);
        context.SetValue(Constants.ContextKeys.ProfileAlias, profile.Alias);
        context.SetValue(Constants.ContextKeys.ProfileVersion, profile.Version);
        context.SetValue(Constants.ContextKeys.ProviderId, profile.Model.ProviderId);
        context.SetValue(Constants.ContextKeys.ModelId, profile.Model.ModelId);
    }
}
