using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy;

public class UmbracoAIArtifactDependency(
    Udi udi,
    ArtifactDependencyMode mode = ArtifactDependencyMode.Exist)
    : ArtifactDependency(udi, false, mode);
