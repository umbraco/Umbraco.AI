using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>One selectable option of an <see cref="AIChoiceDecisionQuestion"/>.</summary>
/// <param name="Key">
/// The caller-owned identifier returned as <see cref="AIChoiceDecisionAnswer.Choice"/> when selected, and
/// used as the option's key in <see cref="AIChoiceDecisionAnswer.Probabilities"/>. It is opaque: providers
/// must return it exactly as sent (ordinal, no case folding, normalization or relabelling), and answers
/// that don't match an offered key exactly are rejected.
/// </param>
/// <param name="Description">An optional, human-readable elaboration of what <paramref name="Key"/> means.</param>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed record AIDecisionOption(string Key, string? Description = null);
