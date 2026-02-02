using Umbraco.AI.Core.Connections;

namespace Umbraco.AI.Persistence.Connections;

/// <summary>
/// Factory for mapping between <see cref="AIConnection"/> domain models and <see cref="AIConnectionEntity"/> database entities.
/// Handles encryption/decryption of sensitive settings fields during the mapping process.
/// </summary>
internal interface IAIConnectionFactory
{
    /// <summary>
    /// Creates an <see cref="AIConnection"/> domain model from a database entity.
    /// Sensitive settings values are automatically decrypted.
    /// </summary>
    /// <param name="entity">The database entity.</param>
    /// <returns>The domain model with decrypted settings.</returns>
    AIConnection BuildDomain(AIConnectionEntity entity);

    /// <summary>
    /// Creates an <see cref="AIConnectionEntity"/> database entity from a domain model.
    /// Sensitive settings values are automatically encrypted based on the provider schema.
    /// </summary>
    /// <param name="connection">The domain model.</param>
    /// <returns>The database entity with encrypted settings.</returns>
    AIConnectionEntity BuildEntity(AIConnection connection);

    /// <summary>
    /// Updates an existing <see cref="AIConnectionEntity"/> with values from a domain model.
    /// Sensitive settings values are automatically encrypted based on the provider schema.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="connection">The domain model with updated values.</param>
    void UpdateEntity(AIConnectionEntity entity, AIConnection connection);
}
