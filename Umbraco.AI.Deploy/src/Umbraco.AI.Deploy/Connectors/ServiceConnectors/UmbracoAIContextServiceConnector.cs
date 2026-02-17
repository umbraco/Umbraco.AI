using System.Text.Json;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Core;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

[UdiDefinition(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi)]
public class UmbracoAIContextServiceConnector(
    IAIContextService contextService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIContextArtifact, AIContext>(settingsAccessor)
{
    private readonly IAIContextService _contextService = contextService;

    protected override int[] ProcessPasses => [2];
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Context;

    public override Task<AIContext?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => _contextService.GetContextAsync(id, cancellationToken);

    public override async IAsyncEnumerable<AIContext> GetEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var contexts = await _contextService.GetContextsAsync(cancellationToken);
        foreach (var context in contexts)
        {
            yield return context;
        }
    }

    public override string GetEntityName(AIContext entity)
        => entity.Name;

    public override Task<AIContextArtifact?> GetArtifactAsync(
        GuidUdi? udi,
        AIContext? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null) return Task.FromResult<AIContextArtifact?>(null);

        var dependencies = new ArtifactDependencyCollection();

        // Serialize Resources as JSON (complex nested structure)
        var resources = entity.Resources.Count > 0
            ? JsonSerializer.SerializeToElement(entity.Resources)
            : (JsonElement?)null;

        var artifact = new AIContextArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Resources = resources,
            DateCreated = entity.DateCreated,
            DateModified = entity.DateModified,
            CreatedByUserId = entity.CreatedByUserId,
            ModifiedByUserId = entity.ModifiedByUserId,
            Version = entity.Version
        };

        return Task.FromResult<AIContextArtifact?>(artifact);
    }

    public override async Task ProcessAsync(
        ArtifactDeployState<AIContextArtifact, AIContext> state,
        IDeployContext context,
        int pass,
        CancellationToken cancellationToken = default)
    {
        state.NextPass = GetNextPass(pass);

        switch (pass)
        {
            case 2:
                await Pass2Async(state, context, cancellationToken);
                break;
        }
    }

    private async Task Pass2Async(
        ArtifactDeployState<AIContextArtifact, AIContext> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Deserialize Resources from JsonElement
        List<AIContextResource>? resources = null;
        if (artifact.Resources.HasValue)
        {
            resources = JsonSerializer.Deserialize<List<AIContextResource>>(artifact.Resources.Value);
        }

        if (state.Entity == null)
        {
            // Create new context
            var aiContext = new AIContext
            {
                Alias = artifact.Alias,
                Name = artifact.Name,
                Resources = resources ?? [],
                CreatedByUserId = artifact.CreatedByUserId,
                ModifiedByUserId = artifact.ModifiedByUserId
            };

            state.Entity = await _contextService.SaveContextAsync(aiContext, cancellationToken);
        }
        else
        {
            // Update existing context
            var aiContext = state.Entity;
            aiContext.Name = artifact.Name;
            aiContext.Resources = resources ?? [];
            aiContext.ModifiedByUserId = artifact.ModifiedByUserId;

            state.Entity = await _contextService.SaveContextAsync(aiContext, cancellationToken);
        }
    }
}
