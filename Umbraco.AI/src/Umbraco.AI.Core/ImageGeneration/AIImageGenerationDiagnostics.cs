namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Diagnostic identifiers for the experimental image-generation API.
/// </summary>
public static class AIImageGenerationDiagnostics
{
    /// <summary>
    /// The diagnostic ID applied via <see cref="System.Diagnostics.CodeAnalysis.ExperimentalAttribute"/>
    /// to the public image-generation surface.
    /// </summary>
    /// <remarks>
    /// The shape of this API may change while Microsoft.Extensions.AI's own image-generation
    /// abstractions remain experimental (<c>MEAI001</c>). Consumers opt in by suppressing this
    /// diagnostic, e.g. <c>#pragma warning disable UMBRACOAI_IMAGEGEN</c> or
    /// <c>&lt;NoWarn&gt;UMBRACOAI_IMAGEGEN&lt;/NoWarn&gt;</c>. This is independent of the
    /// <c>Umbraco:AI:Experimental:ImageGeneration</c> runtime feature flag, which controls whether
    /// the capability is discoverable at all.
    /// </remarks>
    public const string DiagnosticId = "UMBRACOAI_IMAGEGEN";
}
