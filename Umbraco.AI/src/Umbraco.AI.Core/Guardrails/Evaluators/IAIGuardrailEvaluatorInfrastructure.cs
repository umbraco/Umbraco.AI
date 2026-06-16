using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// Defines the infrastructure components required by AI guardrail evaluators.
/// </summary>
public interface IAIGuardrailEvaluatorInfrastructure
{
    /// <summary>
    /// Builder for editable model schemas.
    /// </summary>
    IAIEditableModelSchemaBuilder SchemaBuilder { get; }

    /// <summary>
    /// Resolver for converting stored configuration to typed models, with app-settings resolution.
    /// </summary>
    IAIEditableModelResolver ModelResolver { get; }
}
