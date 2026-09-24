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
    /// <summary>
    /// The real Jev answer path, confirmed against <see href="https://docs.typesafe.ai/api"/> during
    /// T11's live verification (2026-09-24). Shared as a constant — rather than repeating the literal
    /// in <see cref="AnswerPath"/>'s default, <see cref="JevSpikeDecisionCapability.CreateClient"/>'s
    /// blank-settings fallback, and <see cref="JevSpikeDecisionClient"/>'s constructor default — so
    /// the three can't drift apart again the way they did before T11 (when a stale <c>/v1/answer</c>
    /// survived in one of the three after the other two were fixed).
    /// </summary>
    public const string DefaultAnswerPath = "/v1/systemone";

    /// <summary>The Jev API key, resolved the same way any other provider's sensitive field is.</summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>Jev's base URL, confirmed against <see href="https://docs.typesafe.ai/api"/> during T11.</summary>
    [AIField]
    public string? BaseUrl { get; set; } = "https://api.typesafe.ai";

    /// <summary>
    /// The path Jev answers questions on, confirmed against <see href="https://docs.typesafe.ai/api"/>
    /// during T11's live verification (2026-09-24). Kept as a setting, not a hardcoded value, in case
    /// TypeSafe versions or changes it later — defaults to <see cref="DefaultAnswerPath"/>.
    /// </summary>
    [AIField]
    public string? AnswerPath { get; set; } = DefaultAnswerPath;
}
