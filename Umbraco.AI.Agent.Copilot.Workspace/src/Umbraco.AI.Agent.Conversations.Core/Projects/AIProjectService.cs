using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Agent.Conversations.Core.Projects;

/// <summary>
/// Default <see cref="IAIProjectService"/>. Enforces per-user ownership on top of the repository.
/// </summary>
internal sealed class AIProjectService : IAIProjectService
{
    private readonly IAIProjectRepository _repository;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public AIProjectService(
        IAIProjectRepository repository,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _repository = repository;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public async Task<AIProject?> GetProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return null;
        }

        var project = await _repository.GetByIdAsync(id, cancellationToken);
        return project is not null && project.UserKey == userKey.Value ? project : null;
    }

    public async Task<(IReadOnlyList<AIProject> Items, int Total)> GetProjectsPagedAsync(
        int skip,
        int take,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userKey = GetActingUserKeyOrNull();
        if (userKey is null)
        {
            return ([], 0);
        }

        return await _repository.GetPagedAsync(userKey.Value, skip, take, search, cancellationToken);
    }

    public async Task<AIProject> SaveProjectAsync(AIProject project, CancellationToken cancellationToken = default)
    {
        var userKey = GetRequiredActingUserKey();

        if (project.Id != Guid.Empty)
        {
            var existing = await _repository.GetByIdAsync(project.Id, cancellationToken);
            if (existing is not null && existing.UserKey != userKey)
            {
                throw new InvalidOperationException($"Project '{project.Id}' was not found for the acting user.");
            }
        }

        // Never trust a client-supplied UserKey — the acting user owns what they save.
        project.UserKey = userKey;
        return await _repository.SaveAsync(project, cancellationToken);
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectAsync(id, cancellationToken);
        if (project is null)
        {
            // Not-found and not-owned are deliberately indistinguishable so ownership can't be probed.
            throw new InvalidOperationException($"Project '{id}' was not found for the acting user.");
        }

        await _repository.DeleteAsync(id, cancellationToken);
    }

    private Guid? GetActingUserKeyOrNull()
        => _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Key;

    private Guid GetRequiredActingUserKey()
        => GetActingUserKeyOrNull()
            ?? throw new InvalidOperationException("No acting backoffice user is available for this operation.");
}
