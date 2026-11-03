namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Profile settings specific to the extraction capability.
/// </summary>
/// <remarks>
/// Empty today: extraction has no per-profile knobs. It exists so <see cref="AIProfileSettingsSerializer"/>,
/// the Web polymorphic model, and Deploy import all have a real Extraction case instead of silently
/// returning null. Adding a field later is additive.
/// </remarks>
public sealed class AIExtractionProfileSettings : IAIProfileSettings
{
}
