using Umbraco.AI.Core.Guardrails;

namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// Factory for mapping between <see cref="AIGuardrail"/> domain models and <see cref="AIGuardrailEntity"/> database entities.
/// Handles encryption/decryption of sensitive evaluator configuration during the mapping process.
/// </summary>
internal interface IAIGuardrailFactory
{
    /// <summary>
    /// Creates an <see cref="AIGuardrail"/> domain model from a database entity.
    /// Sensitive rule configuration values are automatically decrypted.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model with decrypted rule configuration.</returns>
    AIGuardrail BuildDomain(AIGuardrailEntity entity);

    /// <summary>
    /// Creates an <see cref="AIGuardrailEntity"/> database entity from a domain model.
    /// Sensitive rule configuration values are automatically encrypted based on the evaluator schema.
    /// </summary>
    /// <param name="guardrail">The domain model.</param>
    /// <returns>The database entity with encrypted rule configuration.</returns>
    AIGuardrailEntity BuildEntity(AIGuardrail guardrail);

    /// <summary>
    /// Creates an <see cref="AIGuardrailRuleEntity"/> database entity from a domain model.
    /// Sensitive configuration values are automatically encrypted based on the evaluator schema.
    /// </summary>
    /// <param name="rule">The domain model.</param>
    /// <param name="guardrailId">The parent guardrail ID.</param>
    /// <returns>The database entity with encrypted configuration.</returns>
    AIGuardrailRuleEntity BuildRuleEntity(AIGuardrailRule rule, Guid guardrailId);

    /// <summary>
    /// Updates an existing <see cref="AIGuardrailEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="guardrail">The domain model with updated values.</param>
    void UpdateEntity(AIGuardrailEntity entity, AIGuardrail guardrail);

    /// <summary>
    /// Updates an existing <see cref="AIGuardrailRuleEntity"/> with values from a domain model.
    /// Sensitive configuration values are automatically encrypted based on the evaluator schema.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="rule">The domain model with updated values.</param>
    void UpdateRuleEntity(AIGuardrailRuleEntity entity, AIGuardrailRule rule);
}
