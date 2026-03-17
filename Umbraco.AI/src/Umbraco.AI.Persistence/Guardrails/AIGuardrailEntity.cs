namespace Umbraco.AI.Persistence.Guardrails;

/// <summary>
/// EF Core entity representing an AI guardrail.
/// </summary>
internal class AIGuardrailEntity
{
    /// <summary>
    /// Unique identifier for the guardrail.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias for the guardrail (used for lookup).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the guardrail.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the guardrail was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Date and time when the guardrail was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The key (GUID) of the user who created this guardrail.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this guardrail.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Current version of the guardrail.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Navigation property to the guardrail's rules.
    /// </summary>
    public ICollection<AIGuardrailRuleEntity> Rules { get; set; } = [];
}
