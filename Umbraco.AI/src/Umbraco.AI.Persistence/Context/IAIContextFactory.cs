using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Persistence.Context;

/// <summary>
/// Factory for mapping between <see cref="AIContext"/> domain models and <see cref="AIContextEntity"/> database entities.
/// Handles encryption/decryption of sensitive resource settings during the mapping process.
/// </summary>
internal interface IAIContextFactory
{
    /// <summary>
    /// Creates an <see cref="AIContext"/> domain model from a database entity.
    /// Sensitive resource settings are automatically decrypted.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model with decrypted resource settings.</returns>
    AIContext BuildDomain(AIContextEntity entity);

    /// <summary>
    /// Creates an <see cref="AIContextEntity"/> database entity from a domain model.
    /// Sensitive resource settings are automatically encrypted based on the resource type schema.
    /// </summary>
    /// <param name="context">The domain model.</param>
    /// <returns>The database entity with encrypted resource settings.</returns>
    AIContextEntity BuildEntity(AIContext context);

    /// <summary>
    /// Creates an <see cref="AIContextResourceEntity"/> database entity from a domain model.
    /// Sensitive settings are automatically encrypted based on the resource type schema.
    /// </summary>
    /// <param name="resource">The domain model.</param>
    /// <param name="contextId">The parent context ID.</param>
    /// <returns>The database entity with encrypted settings.</returns>
    AIContextResourceEntity BuildResourceEntity(AIContextResource resource, Guid contextId);

    /// <summary>
    /// Updates an existing <see cref="AIContextEntity"/> with values from a domain model.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="context">The domain model with updated values.</param>
    void UpdateEntity(AIContextEntity entity, AIContext context);

    /// <summary>
    /// Updates an existing <see cref="AIContextResourceEntity"/> with values from a domain model.
    /// Sensitive settings are automatically encrypted based on the resource type schema.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="resource">The domain model with updated values.</param>
    void UpdateResourceEntity(AIContextResourceEntity entity, AIContextResource resource);
}
