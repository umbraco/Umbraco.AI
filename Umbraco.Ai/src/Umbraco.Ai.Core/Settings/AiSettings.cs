using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Represents AI settings configurable via the backoffice.
/// </summary>
public sealed class AiSettings : IAiAuditableEntity
{
    /// <summary>
    /// The fixed ID for the AI settings entity.
    /// </summary>
    public static Guid SettingsId = new Guid("672BF83C-97E0-4D04-9D33-23FC2E5EBE42");

    /// <inheritdoc />
    public Guid Id => SettingsId;
    
    /// <summary>
    /// The ID of the default profile to use for chat operations.
    /// </summary>
    public Guid? DefaultChatProfileId { get; set; }

    /// <summary>
    /// The ID of the default profile to use for embedding operations.
    /// </summary>
    public Guid? DefaultEmbeddingProfileId { get; set; }

    /// <inheritdoc />
    public DateTime DateCreated { get; internal set; }

    /// <inheritdoc />
    public DateTime DateModified { get; internal set; }

    /// <inheritdoc />
    public Guid? CreatedByUserId { get; internal set; }

    /// <inheritdoc />
    public Guid? ModifiedByUserId { get; internal set; }
}
