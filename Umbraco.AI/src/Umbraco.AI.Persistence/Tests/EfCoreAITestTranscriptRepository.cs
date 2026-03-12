using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Core.Tests;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// EF Core implementation of the AI test transcript repository.
/// </summary>
internal class EfCoreAITestTranscriptRepository : IAITestTranscriptRepository
{
    private readonly IEFCoreScopeProvider<UmbracoAIDbContext> _scopeProvider;

    public EfCoreAITestTranscriptRepository(IEFCoreScopeProvider<UmbracoAIDbContext> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <inheritdoc />
    public async Task<AITestTranscript?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        AITestTranscriptEntity? entity = await scope.ExecuteWithContextAsync(async db =>
            await db.TestTranscripts.FirstOrDefaultAsync(t => t.Id == id, cancellationToken));

        scope.Complete();
        return entity is null ? null : AITestTranscriptFactory.BuildDomain(entity);
    }

    /// <inheritdoc />
    public async Task<AITestTranscript> SaveAsync(AITestTranscript transcript, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        var savedTranscript = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestTranscriptEntity? existing = await db.TestTranscripts.FindAsync([transcript.Id], cancellationToken);

            if (existing is null)
            {
                AITestTranscriptEntity newEntity = AITestTranscriptFactory.BuildEntity(transcript);
                db.TestTranscripts.Add(newEntity);
            }
            else
            {
                AITestTranscriptFactory.UpdateEntity(existing, transcript);
            }

            await db.SaveChangesAsync(cancellationToken);
            return transcript;
        });

        scope.Complete();
        return savedTranscript;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using IEfCoreScope<UmbracoAIDbContext> scope = _scopeProvider.CreateScope();

        bool deleted = await scope.ExecuteWithContextAsync(async db =>
        {
            AITestTranscriptEntity? entity = await db.TestTranscripts.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return false;
            }

            db.TestTranscripts.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        });

        scope.Complete();
        return deleted;
    }
}
