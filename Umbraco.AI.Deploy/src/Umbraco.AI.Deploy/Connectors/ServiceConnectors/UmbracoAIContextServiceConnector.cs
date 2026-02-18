using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Contexts, responsible for synchronizing AI Context entities during deploy operations.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi)]
public class UmbracoAIContextServiceConnector(
    IAIContextService contextService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIContextArtifact, AIContext>(settingsAccessor)
{
    /// <inheritdoc />
    protected override int[] ProcessPasses => [2];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Contexts";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Context;

    /// <inheritdoc />
    public override Task<AIContext?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => contextService.GetContextAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIContext> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contexts = await contextService.GetContextsAsync(cancellationToken);
        foreach (var context in contexts)
        {
            yield return context;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIContext entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIContextArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIContext? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AIContextArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        // Serialize Resources as JSON (complex nested structure)
        var resources = entity.Resources.Count > 0
            ? JsonSerializer.SerializeToElement(entity.Resources)
            : (JsonElement?)null;

        var artifact = new AIContextArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Resources = resources
        };

        return Task.FromResult<AIContextArtifact?>(artifact);
    }

    /// <inheritdoc />
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

        // Get or create AIContext entity
        var aiContext = state.Entity
            ?? new AIContext
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name,
            };

        // Update entity properties
        aiContext.Alias = artifact.Alias!;
        aiContext.Name = artifact.Name;
        aiContext.Resources = resources ?? [];

        state.Entity = await contextService.SaveContextAsync(aiContext, cancellationToken);
    }
}
