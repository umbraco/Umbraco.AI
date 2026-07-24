namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Service for project operations. Like <see cref="Conversations.IAIConversationService"/>, every
/// operation is scoped to the acting backoffice user: reads only return the caller's own projects and
/// writes are rejected for projects the caller does not own (F-SEC).
/// </summary>
public interface IAIProjectService
{
    /// <summary>Gets one of the acting user's projects (with resources and context ids) by id, or null.</summary>
    Task<AIProject?> GetProjectAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a paged list of the acting user's projects, newest first.</summary>
    Task<(IReadOnlyList<AIProject> Items, int Total)> GetProjectsPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a project owned by the acting user.</summary>
    Task<AIProject> SaveProjectAsync(AIProject project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes one of the acting user's projects. Its directly-attached resources cascade-delete;
    /// conversations in the project are orphaned (their <c>ProjectId</c> is set null), not deleted.
    /// </summary>
    Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);
}
