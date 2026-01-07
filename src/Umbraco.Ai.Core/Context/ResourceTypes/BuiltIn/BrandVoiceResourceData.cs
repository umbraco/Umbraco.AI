namespace Umbraco.Ai.Core.Context.ResourceTypes.BuiltIn;

/// <summary>
/// Data model for the Brand Voice resource type.
/// </summary>
public sealed class BrandVoiceResourceData
{
    /// <summary>
    /// Description of the tone to use (e.g., "Professional but approachable").
    /// </summary>
    public string? ToneDescription { get; set; }

    /// <summary>
    /// Description of the target audience (e.g., "B2B tech decision makers").
    /// </summary>
    public string? TargetAudience { get; set; }

    /// <summary>
    /// Style guidelines to follow (e.g., "Use active voice, be concise").
    /// </summary>
    public string? StyleGuidelines { get; set; }

    /// <summary>
    /// Patterns and phrases to avoid (e.g., "Jargon, exclamation marks").
    /// </summary>
    public string? AvoidPatterns { get; set; }
}
