using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy;

/// <summary>
/// Represents an artifact dependency for Umbraco.AI entities.
/// Automatically sets checksum validation to false for AI entity dependencies.
/// </summary>
public class UmbracoAIArtifactDependency(
    Udi udi,
    ArtifactDependencyMode mode = ArtifactDependencyMode.Exist)
    : ArtifactDependency(udi, false, mode);
