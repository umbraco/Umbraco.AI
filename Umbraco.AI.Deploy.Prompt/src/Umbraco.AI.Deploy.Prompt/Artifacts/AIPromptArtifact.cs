using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Prompt.Artifacts;

public class AIPromptArtifact(GuidUdi? udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Instructions { get; set; }
    public GuidUdi? ProfileUdi { get; set; }
    public IEnumerable<Guid> ContextIds { get; set; } = [];
    public IEnumerable<string> Tags { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IncludeEntityContext { get; set; }
    public int OptionCount { get; set; }
    public JsonElement? Scope { get; set; }

    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }
    public int Version { get; set; }
}
