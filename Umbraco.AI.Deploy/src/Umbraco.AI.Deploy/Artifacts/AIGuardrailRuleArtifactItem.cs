using System.Text.Json;
using Umbraco.AI.Core.Guardrails;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// The shape of one rule inside <see cref="AIGuardrailArtifact.Rules"/>.
/// Mirrors <see cref="AIGuardrailRule"/> as exported, but keeps the <c>Id</c>, which System.Text.Json
/// cannot set on the entity itself.
/// </summary>
internal sealed class AIGuardrailRuleArtifactItem
{
    public Guid Id { get; init; }

    public required string EvaluatorId { get; init; }

    public required string Name { get; init; }

    public AIGuardrailPhase Phase { get; init; } = AIGuardrailPhase.PostGenerate;

    public AIGuardrailAction Action { get; init; } = AIGuardrailAction.Block;

    public JsonElement? Config { get; init; }

    public string? GuardrailName { get; init; }

    public Guid? GuardrailId { get; init; }

    public int SortOrder { get; init; }

    /// <summary>
    /// Creates the rule with the artifact's Id. An empty Id (older artifacts) is generated on save.
    /// </summary>
    public AIGuardrailRule ToRule()
        => new(Id)
        {
            EvaluatorId = EvaluatorId,
            Name = Name,
            Phase = Phase,
            Action = Action,
            Config = Config,
            GuardrailName = GuardrailName,
            GuardrailId = GuardrailId,
            SortOrder = SortOrder,
        };
}
