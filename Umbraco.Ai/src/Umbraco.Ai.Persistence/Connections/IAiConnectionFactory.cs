using Umbraco.Ai.Core.Connections;

namespace Umbraco.Ai.Persistence.Connections;

/// <summary>
/// Factory for mapping between <see cref="AiConnection"/> domain models and <see cref="AiConnectionEntity"/> database entities.
/// Handles encryption/decryption of sensitive settings fields during the mapping process.
/// </summary>
internal interface IAiConnectionFactory
{
    /// <summary>
    /// Creates an <see cref="AiConnection"/> domain model from a database entity.
    /// Sensitive settings values are automatically decrypted.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model with decrypted settings.</returns>
    AiConnection BuildDomain(AiConnectionEntity entity);

    /// <summary>
    /// Creates an <see cref="AiConnectionEntity"/> database entity from a domain model.
    /// Sensitive settings values are automatically encrypted based on the provider schema.
    /// </summary>
    /// <param name="connection">The domain model.</param>
    /// <returns>The database entity with encrypted settings.</returns>
    AiConnectionEntity BuildEntity(AiConnection connection);

    /// <summary>
    /// Updates an existing <see cref="AiConnectionEntity"/> with values from a domain model.
    /// Sensitive settings values are automatically encrypted based on the provider schema.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="connection">The domain model with updated values.</param>
    void UpdateEntity(AiConnectionEntity entity, AiConnection connection);
}
