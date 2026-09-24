namespace Umbraco.AI.Core.Profiles;

/// <summary>
/// Profile settings specific to the decision capability.
/// </summary>
/// <remarks>
/// Empty today — Jev has no per-profile knobs. It exists so <see cref="AIProfileSettingsSerializer"/>,
/// the Web polymorphic model, and Deploy import all have a real Decision case instead of silently
/// returning null. Adding a field later is additive.
/// </remarks>
public sealed class AIDecisionProfileSettings : IAIProfileSettings
{
}
