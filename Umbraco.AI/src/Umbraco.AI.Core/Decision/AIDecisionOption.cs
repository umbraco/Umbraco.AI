using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>One selectable option of an <see cref="AIChoiceDecisionQuestion"/>.</summary>
/// <param name="Key">The stable identifier returned as <see cref="AIChoiceDecisionResponse.Choice"/> when selected.</param>
/// <param name="Description">An optional, human-readable elaboration of what <paramref name="Key"/> means.</param>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed record AIDecisionOption(string Key, string? Description = null);
