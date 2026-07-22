namespace Umbraco.AI.Agent.Conversations.Core.Projects;

// AIAttachedResource lives in the parent namespace (shared with conversations).

/// <summary>
/// A Copilot Workspace project (domain model): a named container grouping conversations plus reusable
/// context attachments. A project has TWO distinct attachment mechanisms:
/// <list type="bullet">
/// <item><description><see cref="ContextIds"/> — references to existing <c>AIContext</c> entities.</description></item>
/// <item><description><see cref="Resources"/> — its own directly-attached resources.</description></item>
/// </list>
/// Projects are private per user for MVP.
/// </summary>
public sealed class AIProject
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional custom instructions injected into every chat in the project.</summary>
    public string? Instructions { get; set; }

    /// <summary>The key (GUID) of the owning backoffice user.</summary>
    public Guid UserKey { get; set; }

    /// <summary>Referenced <c>AIContext</c> ids (the "attach a context" mechanism).</summary>
    public IList<Guid> ContextIds { get; set; } = [];

    /// <summary>Directly-attached resources (the "attach a direct resource" mechanism).</summary>
    public IList<AIAttachedResource> Resources { get; set; } = [];

    /// <summary>Creation timestamp.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    public DateTime DateModified { get; set; }

    /// <summary>Optimistic-concurrency / change version.</summary>
    public int Version { get; set; } = 1;
}
