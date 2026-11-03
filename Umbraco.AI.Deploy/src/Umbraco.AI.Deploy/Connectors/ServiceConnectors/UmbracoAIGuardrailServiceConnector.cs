using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Deploy;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Service connector for Umbraco AI Guardrails, responsible for synchronizing AI Guardrail entities during deploy operations.
/// </summary>
[UdiDefinition(UmbracoAIConstants.UdiEntityType.Guardrail, UdiType.GuidUdi)]
public class UmbracoAIGuardrailServiceConnector
    : UmbracoAIEntityServiceConnectorBase<AIGuardrailArtifact, AIGuardrail>
{
    private readonly IAIGuardrailService _guardrailService;
    private readonly AIGuardrailEvaluatorCollection _evaluators;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIGuardrailServiceConnector"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an AIGuardrailEvaluatorCollection. Will be removed in v20.")]
    public UmbracoAIGuardrailServiceConnector(
        IAIGuardrailService guardrailService,
        UmbracoAIDeploySettingsAccessor settingsAccessor)
        : this(
            guardrailService,
            settingsAccessor,
            StaticServiceProvider.Instance.GetRequiredService<AIGuardrailEvaluatorCollection>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIGuardrailServiceConnector"/> class.
    /// </summary>
    public UmbracoAIGuardrailServiceConnector(
        IAIGuardrailService guardrailService,
        UmbracoAIDeploySettingsAccessor settingsAccessor,
        AIGuardrailEvaluatorCollection evaluators)
        : base(settingsAccessor)
    {
        _guardrailService = guardrailService;
        _evaluators = evaluators;
    }

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
        => _guardrailService.GetGuardrailAsync(id, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<AIGuardrail> GetEntitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var guardrails = await _guardrailService.GetGuardrailsAsync(cancellationToken);
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
            ? SerializeRules(entity.Rules)
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
        IList<AIGuardrailRule> targetRules = state.Entity?.Rules ?? [];

        // Deserialize Rules from JsonElement
        List<AIGuardrailRule>? rules = null;
        if (artifact.Rules.HasValue)
        {
            rules = artifact.Rules.Value.Deserialize<List<AIGuardrailRuleArtifactItem>>()
                ?.Select(item => item.ToRule())
                .ToList();
        }

        // Sensitive literals never cross environments; keep this environment's own values instead.
        foreach (AIGuardrailRule rule in rules ?? [])
        {
            object? config = UmbracoAISensitiveSettingsFilter.ImportSettings(
                rule.Config,
                targetRules.FirstOrDefault(r => r.Id == rule.Id)?.Config,
                GetConfigSchema(rule.EvaluatorId));
            rule.Config = config is null ? null : (JsonElement)config;
        }

        // Get or create AIGuardrail entity
        var guardrail = state.Entity
            ?? new AIGuardrail(artifact.Udi.Guid)
            {
                Alias = artifact.Alias!,
                Name = artifact.Name,
            };

        // Update entity properties
        guardrail.Alias = artifact.Alias!;
        guardrail.Name = artifact.Name;
        guardrail.Rules = rules ?? [];

        state.Entity = await _guardrailService.SaveGuardrailAsync(guardrail, cancellationToken);
    }

    private JsonElement SerializeRules(IList<AIGuardrailRule> rules)
    {
        var nodes = JsonSerializer.SerializeToNode(rules)!.AsArray();
        for (var i = 0; i < rules.Count; i++)
        {
            object? original = rules[i].Config;
            object? config = UmbracoAISensitiveSettingsFilter.RemoveSensitiveLiterals(
                original,
                GetConfigSchema(rules[i].EvaluatorId));

            if (!ReferenceEquals(config, original))
            {
                nodes[i]![nameof(AIGuardrailRule.Config)] = JsonSerializer.SerializeToNode(config);
            }
        }

        return JsonSerializer.SerializeToElement(nodes);
    }

    private AIEditableModelSchema? GetConfigSchema(string evaluatorId)
        => _evaluators.GetById(evaluatorId)?.GetConfigSchema();
}
