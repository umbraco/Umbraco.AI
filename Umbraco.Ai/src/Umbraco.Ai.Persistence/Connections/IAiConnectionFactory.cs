using Umbraco.Ai.Core.Connections;
using Umbraco.Ai.Core.EditableModels;

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

    /// <summary>
    /// Creates a JSON snapshot of a connection with sensitive settings encrypted.
    /// Used for version history storage.
    /// </summary>
    /// <param name="connection">The connection to snapshot.</param>
    /// <returns>JSON string with encrypted sensitive settings.</returns>
    string CreateSnapshot(AiConnection connection);

    /// <summary>
    /// Creates a domain model from a JSON snapshot, decrypting sensitive settings.
    /// Used for restoring version history.
    /// </summary>
    /// <param name="json">The JSON snapshot.</param>
    /// <returns>The connection domain model with decrypted settings, or null if parsing fails.</returns>
    AiConnection? BuildDomainFromSnapshot(string json);
}
