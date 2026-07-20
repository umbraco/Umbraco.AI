using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Context.Models;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;

/// <summary>
/// API request model for creating or updating a project. The owning user is taken from the acting
/// backoffice user, never the client.
/// </summary>
public sealed class ProjectRequestModel
{
    /// <summary>Display name.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional custom instructions injected into every chat in the project.</summary>
    public string? Instructions { get; set; }

    /// <summary>Referenced <c>AIContext</c> ids (the "attach a context" mechanism).</summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>Directly-attached resources (the "attach a direct resource" mechanism).</summary>
    public IEnumerable<ContextResourceModel> Resources { get; set; } = [];
}
