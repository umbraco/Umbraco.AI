using System.Text;

namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type for brand voice definitions including tone, audience, style, and patterns to avoid.
/// </summary>
[AIContextResourceType("brand-voice", "Brand Voice")]
public sealed class BrandVoiceResourceType : AIContextResourceTypeBase<BrandVoiceResourceData>
{
    /// <inheritdoc />
    public override string? Description => "Define tone, audience, style guidelines, and patterns to avoid";

    /// <inheritdoc />
    public override string? Icon => "icon-voice";

    /// <summary>
    /// Initializes a new instance of the <see cref="BrandVoiceResourceType"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    public BrandVoiceResourceType(IAiContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    protected override string FormatForLlm(BrandVoiceResourceData data)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(data.ToneDescription))
            sb.AppendLine($"Tone: {data.ToneDescription}");

        if (!string.IsNullOrWhiteSpace(data.TargetAudience))
            sb.AppendLine($"Audience: {data.TargetAudience}");

        if (!string.IsNullOrWhiteSpace(data.StyleGuidelines))
            sb.AppendLine($"Style: {data.StyleGuidelines}");

        if (!string.IsNullOrWhiteSpace(data.AvoidPatterns))
            sb.AppendLine($"Avoid: {data.AvoidPatterns}");

        return sb.ToString().TrimEnd();
    }
}
