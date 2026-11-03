using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Contexts, responsible for synchronizing AI Context entities during deploy operations.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Context, UdiType.GuidUdi)]
public class UmbracoAIContextServiceConnector
    : UmbracoAIEntityServiceConnectorBase<AIContextArtifact, AIContext>
{
    private readonly IAIContextService _contextService;
    private readonly AIContextResourceTypeCollection _resourceTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIContextServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an AIContextResourceTypeCollection. Will be removed in v20.")]
    public UmbracoAIContextServiceConnector(
        IAIContextService contextService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            contextService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<AIContextResourceTypeCollection>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIContextServiceConnector"/> class.
    /// </summary>
    public UmbracoAIContextServiceConnector(
        IAIContextService contextService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        AIContextResourceTypeCollection resourceTypes)
        : base(settingsAccessor)
    {
        _contextService = contextService;
        _resourceTypes = resourceTypes;
    }

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
        => _contextService.GetContextAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIContext> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var contexts = await _contextService.GetContextsAsync(cancellationToken);
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
            ? SerializeResources(entity.Resources)
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
        IList<AIContextResource> targetResources = state.Entity?.Resources ?? [];

        // Deserialize Resources from JsonElement
        List<AIContextResource>? resources = null;
        if (artifact.Resources.HasValue)
        {
            resources = artifact.Resources.Value.Deserialize<List<AIContextResourceArtifactItem>>()
                ?.Select(item => item.ToResource())
                .ToList();
        }

        // Sensitive literals never cross environments; keep this environment's own values instead.
        foreach (AIContextResource resource in resources ?? [])
        {
            resource.Settings = UmbracoAISensitiveSettingsFilter.ImportSettings(
                resource.Settings,
                targetResources.FirstOrDefault(r => r.Id == resource.Id)?.Settings,
                GetSettingsSchema(resource.ResourceTypeId));
        }

        // Get or create AIContext entity
        var aiContext = state.Entity
            ?? new AIContext(artifact.Udi.Guid)
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
            };

        // Update entity properties
        aiContext.Alias = artifact.Alias!;
        aiContext.Name = artifact.Name;
        aiContext.Resources = resources ?? [];

        state.Entity = await _contextService.SaveContextAsync(aiContext, cancellationToken);
    }

    private JsonElement SerializeResources(IList<AIContextResource> resources)
    {
        var nodes = JsonSerializer.SerializeToNode(resources)!.AsArray();
        for (var i = 0; i < resources.Count; i++)
        {
            object? original = resources[i].Settings;
            object? settings = UmbracoAISensitiveSettingsFilter.RemoveSensitiveLiterals(
                original,
                GetSettingsSchema(resources[i].ResourceTypeId));

            if (!ReferenceEquals(settings, original))
            {
                nodes[i]![nameof(AIContextResource.Settings)] = JsonSerializer.SerializeToNode(settings);
            }
        }

        return JsonSerializer.SerializeToElement(nodes);
    }

    private AIEditableModelSchema? GetSettingsSchema(string resourceTypeId)
        => _resourceTypes.GetById(resourceTypeId)?.GetSettingsSchema();
}
