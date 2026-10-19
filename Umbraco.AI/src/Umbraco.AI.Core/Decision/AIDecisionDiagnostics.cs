namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Diagnostic identifiers for the experimental decision API.
/// </summary>
public static class AIDecisionDiagnostics
{
    /// <summary>
    /// The diagnostic ID applied via <see cref="System.Diagnostics.CodeAnalysis.ExperimentalAttribute"/>
    /// to the public decision surface.
    /// </summary>
    /// <remarks>
    /// The shape of this API may change while the underlying decision-model architecture is still being
    /// validated. Consumers opt in by suppressing this diagnostic, e.g.
    /// <c>#pragma warning disable UMBRACOAI_DECISION</c> or
    /// <c>&lt;NoWarn&gt;UMBRACOAI_DECISION&lt;/NoWarn&gt;</c>. This is independent of the
    /// <c>Umbraco:AI:Experimental:Decision</c> runtime feature flag, which controls whether the
    /// capability is discoverable at all.
    /// </remarks>
    public const string DiagnosticId = "UMBRACOAI_DECISION";
}
