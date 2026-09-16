namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for AI-suggested starter prompts. Drafts only — nothing is persisted.
/// </summary>
public class SuggestStartersResponseModel
{
    /// <summary>
    /// The suggested starter prompts, already clamped to the same 4 × 200-character limits enforced
    /// when saving an agent.
    /// </summary>
    public IReadOnlyList<string> Starters { get; set; } = [];
}
