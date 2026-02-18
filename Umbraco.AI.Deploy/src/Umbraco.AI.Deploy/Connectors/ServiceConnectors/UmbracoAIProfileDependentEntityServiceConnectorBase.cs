using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Base class for AI entities that optionally depend on AIProfile.
/// Provides helper methods for profile dependency management and Pass 2/4 resolution pattern.
/// </summary>
public abstract class UmbracoAIProfileDependentEntityServiceConnectorBase<TArtifact, TEntity>(
    IAIProfileService profileService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<TArtifact, TEntity>(settingsAccessor)
    where TArtifact : DeployArtifactBase<GuidUdi>
    where TEntity : IAIVersionableEntity
{
    /// <summary>
    /// Service to manage AI profiles, used for resolving profile dependencies.
    /// </summary>
    private readonly IAIProfileService _profileService = profileService;

    /// <summary>
    /// Profile-dependent entities use Pass 2/4 pattern.
    /// Pass 2: Create/update entity with basic properties
    /// Pass 4: Resolve optional ProfileId from ProfileUdi
    /// </summary>
    protected override int[] ProcessPasses => [2, 4];

    /// <summary>
    /// Helper to add optional profile dependency to artifact.
    /// </summary>
    protected GuidUdi? AddProfileDependency(
        Guid? profileId,
        ArtifactDependencyCollection dependencies)
    {
        if (!profileId.HasValue)
        {
            return null;
        }

        var profileUdi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Profile, profileId.Value);
        dependencies.Add(new UmbracoAIArtifactDependency(profileUdi, ArtifactDependencyMode.Match));
        return profileUdi;
    }

    /// <summary>
    /// Helper to resolve optional profile ID from UDI during Pass 4.
    /// </summary>
    protected async Task<Guid?> ResolveProfileIdAsync(
        GuidUdi? profileUdi,
        CancellationToken ct)
    {
        if (profileUdi == null)
        {
            return null;
        }

        profileUdi.EnsureType(UmbracoAIConstants.UdiEntityType.Profile);

        var profile = await _profileService.GetProfileAsync(profileUdi.Guid, ct);
        return profile?.Id;
    }
}
