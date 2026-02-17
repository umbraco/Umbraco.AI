using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;

namespace Umbraco.AI.Deploy.Agent.Artifacts;

public class AIAgentArtifact(GuidUdi? udi, IEnumerable<ArtifactDependency>? dependencies = null)
    : DeployArtifactBase<GuidUdi>(udi, dependencies)
{
    public required string Alias { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public GuidUdi? ProfileUdi { get; set; }
    public IEnumerable<Guid> ContextIds { get; set; } = [];
    public IEnumerable<string> SurfaceIds { get; set; } = [];
    public JsonElement? Scope { get; set; }
    public IEnumerable<string> AllowedToolIds { get; set; } = [];
    public IEnumerable<string> AllowedToolScopeIds { get; set; } = [];
    public JsonElement? UserGroupPermissions { get; set; }
    public string? Instructions { get; set; }
    public bool IsActive { get; set; }

    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ModifiedByUserId { get; set; }
    public int Version { get; set; }
}
