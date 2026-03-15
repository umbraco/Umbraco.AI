using System.Runtime.CompilerServices;
using System.Text.Json;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Guardrails, responsible for synchronizing AI Guardrail entities during deploy operations.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Guardrail, UdiType.GuidUdi)]
public class UmbracoAIGuardrailServiceConnector(
    IAIGuardrailService guardrailService,
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : UmbracoAIEntityServiceConnectorBase<AIGuardrailArtifact, AIGuardrail>(settingsAccessor)
{
    /// <inheritdoc />
    protected override int[] ProcessPasses => [2];

    /// <inheritdoc />
    protected override string[] ValidOpenSelectors => ["this", "this-and-descendants", "descendants"];

    /// <inheritdoc />
    protected override string OpenUdiName => "All Umbraco AI Guardrails";

    /// <inheritdoc />
    public override string UdiEntityType => UmbracoAIConstants.UdiEntityType.Guardrail;

    /// <inheritdoc />
    public override Task<AIGuardrail?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => guardrailService.GetGuardrailAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIGuardrail> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var guardrails = await guardrailService.GetAllGuardrailsAsync(cancellationToken);
        foreach (var guardrail in guardrails)
        {
            yield return guardrail;
        }
    }

    /// <inheritdoc />
    public override string GetEntityName(AIGuardrail entity)
        => entity.Name;

    /// <inheritdoc />
    public override Task<AIGuardrailArtifact?> GetArtifactAsync(
        GuidUdi udi,
        AIGuardrail? entity,
        CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            return Task.FromResult<AIGuardrailArtifact?>(null);
        }

        var dependencies = new ArtifactDependencyCollection();

        // Serialize Rules as JSON (complex nested structure with evaluator configs)
        var rules = entity.Rules.Count > 0
            ? JsonSerializer.SerializeToElement(entity.Rules)
            : (JsonElement?)null;

        var artifact = new AIGuardrailArtifact(udi, dependencies)
        {
            Alias = entity.Alias,
            Name = entity.Name,
            Rules = rules
        };

        return Task.FromResult<AIGuardrailArtifact?>(artifact);
    }

    /// <inheritdoc />
    public override async Task ProcessAsync(
        ArtifactDeployState<AIGuardrailArtifact, AIGuardrail> state,
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
        ArtifactDeployState<AIGuardrailArtifact, AIGuardrail> state,
        IDeployContext context,
        CancellationToken cancellationToken)
    {
        var artifact = state.Artifact;

        // Deserialize Rules from JsonElement
        List<AIGuardrailRule>? rules = null;
        if (artifact.Rules.HasValue)
        {
            rules = artifact.Rules.Value.Deserialize<List<AIGuardrailRule>>();
        }

        // Get or create AIGuardrail entity
        var guardrail = state.Entity
            ?? new AIGuardrail
            {
                Id = artifact.Udi.Guid,
                Alias = artifact.Alias!,
                Name = artifact.Name,
            };

        // Update entity properties
        guardrail.Alias = artifact.Alias!;
        guardrail.Name = artifact.Name;
        guardrail.Rules = rules ?? [];

        state.Entity = await guardrailService.SaveGuardrailAsync(guardrail, cancellationToken);
    }
}
