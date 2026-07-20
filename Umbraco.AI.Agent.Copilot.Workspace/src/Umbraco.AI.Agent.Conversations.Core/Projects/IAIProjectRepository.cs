namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Repository for project persistence (project + its directly-attached resources). Internal
/// implementation detail — external callers go through the service layer.
/// </summary>
internal interface IAIProjectRepository
{
    /// <summary>Gets a project (with its resources and context ids) by id, or null.</summary>
    Task<AIProject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a paged list of a user's projects, newest first, optionally filtered by name.</summary>
    Task<(IReadOnlyList<AIProject> Items, int Total)> GetPagedAsync(
        Guid userKey,
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a project. On update, reconciles the resource collection (adds new, updates
    /// existing, removes deleted) and bumps <see cref="AIProject.Version"/>.
    /// </summary>
    Task<AIProject> SaveAsync(AIProject project, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project. Its directly-attached resources cascade-delete; conversations are orphaned
    /// (their <c>ProjectId</c> is set null) rather than deleted.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
