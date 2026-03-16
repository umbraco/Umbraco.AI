using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;

namespace Umbraco.AI.Core.Guardrails;

/// <summary>
/// Represents a guardrail containing rules that evaluate AI inputs and responses
/// for safety, compliance, and quality enforcement.
/// </summary>
/// <remarks>
/// Guardrails are standalone, reusable entities that can be assigned to profiles, prompts,
/// and agents. They contain multiple rules, each referencing a registered evaluator with
/// its phase (pre-generate or post-generate) and action (block or warn).
/// </remarks>
public sealed class AIGuardrail : IAIVersionableEntity
{
    /// <summary>
    /// The unique identifier of the guardrail.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// The alias of the guardrail (e.g., "content-safety").
    /// Used for programmatic reference.
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// The display name of the guardrail (e.g., "Content Safety Policy").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The date and time when the guardrail was created.
    /// </summary>
    public DateTime DateCreated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the guardrail was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The key (GUID) of the user who created this guardrail.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this guardrail.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// The current version of the guardrail.
    /// Starts at 1 and increments with each save operation.
    /// </summary>
    public int Version { get; internal set; } = 1;

    /// <summary>
    /// The rules belonging to this guardrail.
    /// Rules are ordered by <see cref="AIGuardrailRule.SortOrder"/>.
    /// </summary>
    public IList<AIGuardrailRule> Rules { get; set; } = [];
}
