using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// Connection settings for <see cref="JevSpikeProvider"/> — the disposable Decision-capability spike
/// (see the decision-capability plan's T10). Not a real, shippable provider: see
/// <see cref="JevSpikeProvider"/>'s remarks for why this lives in a test-support project instead of
/// <c>src/</c>.
/// </summary>
public sealed class JevSpikeProviderSettings
{
    /// <summary>The Jev API key, resolved the same way any other provider's sensitive field is.</summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>Jev's base URL. Defaults to a placeholder host — nobody has confirmed Jev's real one yet.</summary>
    [AIField]
    public string? BaseUrl { get; set; } = "https://api.typesafe.ai";

    /// <summary>
    /// The path Jev answers questions on. A placeholder (<c>/v1/answer</c>) pending confirmation
    /// against Jev's real API docs — kept as a setting, not a constant, so T11's manual verification
    /// can repoint it without a code change if the real path turns out to differ.
    /// </summary>
    [AIField]
    public string? AnswerPath { get; set; } = "/v1/answer";
}
