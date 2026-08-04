using Umbraco.AI.Web.Api.Management.Context.Models;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;

/// <summary>
/// API response model for a project (with its referenced contexts and directly-attached resources).
/// </summary>
public sealed class ProjectResponseModel
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional custom instructions injected into every chat in the project.</summary>
    public string? Instructions { get; set; }

    /// <summary>Referenced <c>AIContext</c> ids (the "attach a context" mechanism).</summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>Directly-attached resources (the "attach a direct resource" mechanism).</summary>
    public IEnumerable<ContextResourceModel> Resources { get; set; } = [];

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime DateModified { get; set; }
}
