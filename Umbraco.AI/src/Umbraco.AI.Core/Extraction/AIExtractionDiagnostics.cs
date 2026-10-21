namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Diagnostic identifiers for the experimental extraction API.
/// </summary>
public static class AIExtractionDiagnostics
{
    /// <summary>
    /// The diagnostic ID applied via <see cref="System.Diagnostics.CodeAnalysis.ExperimentalAttribute"/>
    /// to the public extraction surface.
    /// </summary>
    /// <remarks>
    /// The shape of this API may change while it is still being validated. Consumers opt in by
    /// suppressing this diagnostic, e.g. <c>#pragma warning disable UMBRACOAI_EXTRACTION</c> or
    /// <c>&lt;NoWarn&gt;UMBRACOAI_EXTRACTION&lt;/NoWarn&gt;</c>. This is independent of the
    /// <c>Umbraco:AI:Experimental:Extraction</c> runtime feature flag, which controls whether the
    /// capability is discoverable at all.
    /// </remarks>
    public const string DiagnosticId = "UMBRACOAI_EXTRACTION";
}
