namespace Umbraco.AI.Agent.Conversations.Persistence.Projects;

/// <summary>
/// EF Core entity for a project (a named container grouping conversations plus reusable context
/// attachments).
/// </summary>
internal class AIProjectEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional custom instructions injected into every chat in the project.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// The key (GUID) of the owning backoffice user. Projects are private per user for MVP.
    /// </summary>
    public Guid UserKey { get; set; }

    /// <summary>
    /// JSON-serialized array of referenced <c>AIContext</c> ids (the "attach a context" mechanism).
    /// </summary>
    public string? ContextIds { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// Optimistic-concurrency / change version.
    /// </summary>
    public int Version { get; set; } = 1;
}
