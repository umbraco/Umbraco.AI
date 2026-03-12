using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Persistence.Tests;

/// <summary>
/// Factory for mapping between <see cref="AITest"/> domain models and <see cref="AITestEntity"/> database entities.
/// Handles encryption/decryption of sensitive config fields during the mapping process.
/// </summary>
internal interface IAITestFactory
{
    /// <summary>
    /// Creates an <see cref="AITest"/> domain model from a database entity.
    /// Sensitive config values are automatically decrypted.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model with decrypted config values.</returns>
    AITest BuildDomain(AITestEntity entity);

    /// <summary>
    /// Creates an <see cref="AITestEntity"/> database entity from a domain model.
    /// Sensitive config values are automatically encrypted based on the feature/grader schemas.
    /// </summary>
    /// <param name="test">The domain model.</param>
    /// <returns>The database entity with encrypted config values.</returns>
    AITestEntity BuildEntity(AITest test);

    /// <summary>
    /// Updates an existing <see cref="AITestEntity"/> with values from a domain model.
    /// Sensitive config values are automatically encrypted based on the feature/grader schemas.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="test">The domain model with updated values.</param>
    void UpdateEntity(AITestEntity entity, AITest test);
}
