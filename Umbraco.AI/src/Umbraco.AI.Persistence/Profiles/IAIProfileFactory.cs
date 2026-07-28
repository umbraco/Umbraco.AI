using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Persistence.Profiles;

/// <summary>
/// Factory for mapping between <see cref="AIProfile"/> domain models and <see cref="AIProfileEntity"/> database entities.
/// Handles serialization (and encryption of sensitive fields) of provider-declared profile settings.
/// </summary>
internal interface IAIProfileFactory
{
    /// <summary>
    /// Creates an <see cref="AIProfile"/> domain model from a database entity.
    /// Provider-declared profile settings are decrypted during deserialization.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model.</returns>
    AIProfile BuildDomain(AIProfileEntity entity);

    /// <summary>
    /// Creates an <see cref="AIProfileEntity"/> database entity from a domain model.
    /// Sensitive provider-declared profile settings are encrypted based on the provider schema.
    /// </summary>
    /// <param name="profile">The domain model.</param>
    /// <returns>The database entity.</returns>
    AIProfileEntity BuildEntity(AIProfile profile);

    /// <summary>
    /// Updates an existing <see cref="AIProfileEntity"/> with values from a domain model.
    /// Sensitive provider-declared profile settings are encrypted based on the provider schema.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="profile">The domain model with updated values.</param>
    void UpdateEntity(AIProfileEntity entity, AIProfile profile);
}
