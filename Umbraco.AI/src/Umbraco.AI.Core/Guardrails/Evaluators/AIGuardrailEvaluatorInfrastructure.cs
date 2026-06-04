using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Default implementation of <see cref="IAIGuardrailEvaluatorInfrastructure"/>.
/// </summary>
internal sealed class AIGuardrailEvaluatorInfrastructure(
    IAIEditableModelSchemaBuilder schemaBuilder,
    IAIEditableModelResolver modelResolver)
    : IAIGuardrailEvaluatorInfrastructure
{
    /// <inheritdoc />
    public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = schemaBuilder;

    /// <inheritdoc />
    public IAIEditableModelResolver ModelResolver { get; } = modelResolver;
}
