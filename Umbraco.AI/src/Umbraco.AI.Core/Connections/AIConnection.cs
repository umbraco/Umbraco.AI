using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Represents a connection to an AI provider with credentials and settings.
/// </summary>
public class AIConnection : IAIVersionableEntity
{
    private Guid _id;

    /// <summary>
    /// Unique identifier for the connection.
    /// </summary>
    /// <remarks>
    /// Leave empty to have an ID generated when the connection is first saved. Set it when creating a
    /// connection that must keep an ID from elsewhere, such as one synced from another environment.
    /// </remarks>
    public Guid Id
    {
        get => _id;
        init => _id = value;
    }

    /// <summary>
    /// Unique alias for the connection (used for programmatic lookup).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the connection (shown in UI).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The ID of the provider this connection is for (e.g., "openai", "azure").
    /// Must match a registered provider's ID.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Provider-specific settings (credentials, endpoints, etc.).
    /// Type depends on provider (e.g., OpenAIProviderSettings).
    /// </summary>
    public object? Settings { get; set; }

    /// <summary>
    /// Whether this connection is currently active/enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the connection was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the connection was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this connection.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this connection.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version of the connection.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;

    /// <summary>
    /// Assigns the ID of a new connection. Used by the service when saving a connection with no ID.
    /// </summary>
    internal void SetId(Guid id) => _id = id;
}
